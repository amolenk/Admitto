using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeCouponEmail;

internal interface ICouponEmailComposer
{
    ValueTask ComposeAsync(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        CouponEmailIntent intent,
        CouponEmailDelivery delivery,
        CancellationToken cancellationToken = default);
}

internal abstract record CouponEmailIntent;

internal sealed record CouponInvitationIntent(string CouponCode) : CouponEmailIntent;

internal sealed record WaitlistOfferIntent(
    string CouponCode,
    string TicketTypeName,
    DateTimeOffset ExpiresAt) : CouponEmailIntent;

internal sealed record CouponEmailDelivery(
    string RecipientAddress,
    string RecipientName,
    string IdempotencyKey);

internal sealed class CouponEmailComposer(
    IEmailWriteStore writeStore,
    IEventEmailRenderingContextProvider eventContextProvider,
    IEmailPreparationService preparationService,
    ICommandHandler<PrepareEmailDeliveryCommand> prepareDeliveryHandler)
    : ICouponEmailComposer
{
    public async ValueTask ComposeAsync(
        TeamId teamId,
        TicketedEventId ticketedEventId,
        CouponEmailIntent intent,
        CouponEmailDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        var recipient = EmailAddress.From(delivery.RecipientAddress);
        var existing = await writeStore.EmailLog
            .AsNoTracking()
            .FirstOrDefaultAsync(
                log => log.TeamId == teamId
                    && log.TicketedEventId == ticketedEventId
                    && log.Recipient == recipient
                    && log.IdempotencyKey == delivery.IdempotencyKey,
                cancellationToken);

        // Check terminal claims before loading projections so redelivery remains a no-op
        // even if the event context has since disappeared.
        if (existing?.IsTerminal == true)
            return;

        var context = await eventContextProvider.GetScopeAsync(
            teamId,
            ticketedEventId,
            cancellationToken);
        var links = RegistrationEmailLinks.From(context.PublicEventLink, registrationId: null);

        var (emailType, parameters) = intent switch
        {
            CouponInvitationIntent invitation =>
                (BuiltInEmailTemplateNames.CouponInvitation, new Dictionary<string, object?>
                {
                    ["team_name"] = context.TeamName,
                    ["event_name"] = context.EventName,
                    ["event_website"] = context.WebsiteUrl,
                    ["coupon_code"] = invitation.CouponCode,
                    ["register_link"] = links.RegisterLink
                }),
            WaitlistOfferIntent offer =>
                (BuiltInEmailTemplateNames.WaitlistNotification, new Dictionary<string, object?>
                {
                    ["event_name"] = context.EventName,
                    ["event_website"] = context.WebsiteUrl,
                    ["coupon_code"] = offer.CouponCode,
                    ["ticket_type_name"] = offer.TicketTypeName,
                    ["expires_at"] = offer.ExpiresAt.ToString("f"),
                    ["register_link"] = links.RegisterLink
                }),
            _ => throw new ArgumentOutOfRangeException(
                nameof(intent),
                intent,
                "Unsupported coupon email intent.")
        };

        var rendered = await preparationService.PrepareAsync(
            emailType,
            teamId,
            ticketedEventId,
            parameters,
            context.AccentColor,
            cancellationToken);

        // PrepareEmailDelivery is the single claim/outbox boundary. Its terminal check
        // also protects the race between this pre-check and claim preparation.
        await prepareDeliveryHandler.HandleAsync(
            new PrepareEmailDeliveryCommand(
                teamId.Value,
                ticketedEventId.Value,
                delivery.RecipientAddress,
                delivery.RecipientName,
                emailType,
                delivery.IdempotencyKey,
                rendered.Subject,
                rendered.TextBody,
                rendered.HtmlBody,
                RegistrationId: null),
            cancellationToken);
    }
}
