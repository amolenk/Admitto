using System.Globalization;
using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.Email.Application.Composing;

internal sealed record RenderedTransactionalEmail(
    string EmailType,
    string Subject,
    string TextBody,
    string HtmlBody);

internal interface ITransactionalEmailComposer
{
    ValueTask<RenderedTransactionalEmail> ComposeAsync(
        TransactionalEmailIntent intent,
        CancellationToken cancellationToken = default);

    ValueTask<ReconfirmationEmailCompositionScope> CreateReconfirmationScopeAsync(
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default);
}

internal sealed class TransactionalEmailComposer(
    IEmailReadStore readStore,
    IEmailRenderer renderer,
    IOptions<PublicEventLinksOptions> publicEventLinksOptions)
    : ITransactionalEmailComposer
{
    public async ValueTask<RenderedTransactionalEmail> ComposeAsync(
        TransactionalEmailIntent intent,
        CancellationToken cancellationToken = default)
    {
        var context = await LoadContextAsync(intent.TeamId, intent.TicketedEventId, cancellationToken);

        var (emailType, parameters) = intent switch
        {
            TicketConfirmationIntent value => (
                value.TicketTypes.Count > 0
                    ? BuiltInEmailTemplateNames.TicketConfirmation
                    : BuiltInEmailTemplateNames.WaitlistConfirmation,
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["first_name"] = value.FirstName,
                    ["team_name"] = context.TeamName,
                    ["event_name"] = context.EventName,
                    ["public_event_link"] = context.GetLinks(value.RegistrationId).PublicEventLink,
                    ["qrcode_link"] = context.GetLinks(value.RegistrationId).QRCodeLink,
                    ["edit_registration_link"] = context.GetLinks(value.RegistrationId).EditRegistrationLink,
                    ["ticket_types"] = value.TicketTypes,
                    ["waitlisted_ticket_types"] = value.WaitlistedTicketTypes
                }),
            CouponInvitationIntent value => (
                BuiltInEmailTemplateNames.CouponInvitation,
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["team_name"] = context.TeamName,
                    ["event_name"] = context.EventName,
                    ["event_website"] = context.WebsiteUrl,
                    ["coupon_code"] = value.CouponCode,
                    ["register_link"] = context.GetLinks(null).RegisterLink
                }),
            WaitlistOfferIntent value => (
                BuiltInEmailTemplateNames.WaitlistNotification,
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["event_name"] = context.EventName,
                    ["event_website"] = context.WebsiteUrl,
                    ["coupon_code"] = value.CouponCode,
                    ["ticket_type_name"] = value.TicketTypeName,
                    ["expires_at"] = FormatWithTimeZone(value.ExpiresAt, context.TimeZone),
                    ["intro_text"] = WaitlistOfferIntroText(value.Reason, context.EventName),
                    ["expiry_note"] = WaitlistOfferExpiryNote(value.Reason),
                    ["cta_link"] = value.RegistrationId is { } registrationId
                        ? context.GetLinks(registrationId).EditRegistrationLink
                        : context.GetLinks(null).RegisterLink,
                    ["cta_label"] = value.RegistrationId is not null ? "View Your Registration" : "Register Now"
                }),
            WaitlistOfferExpiredIntent value => (
                BuiltInEmailTemplateNames.WaitlistOfferExpired,
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["event_name"] = context.EventName,
                    ["event_website"] = context.WebsiteUrl,
                    ["ticket_type_name"] = value.TicketTypeName,
                    ["registration_closed"] = value.RegistrationClosed
                }),
            AttendeeRequestCancellationIntent value => (
                BuiltInEmailTemplateNames.Cancellation,
                CancellationParameters(value.FirstName, context, value.RegistrationId)),
            WaitlistCancellationIntent value => (
                BuiltInEmailTemplateNames.WaitlistCancellation,
                CancellationParameters(value.FirstName, context, value.RegistrationId)),
            ReconfirmAutoCancellationIntent value => (
                BuiltInEmailTemplateNames.ReconfirmCancelled,
                CancellationParameters(value.FirstName, context, value.RegistrationId)),
            VisaLetterDeniedCancellationIntent value => (
                BuiltInEmailTemplateNames.VisaLetterDenied,
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["first_name"] = value.FirstName,
                    ["event_name"] = context.EventName
                }),
            VerificationCodeIntent value => (
                BuiltInEmailTemplateNames.VerificationCode,
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["plain_code"] = value.PlainCode,
                    ["event_name"] = context.EventName,
                    ["team_name"] = context.TeamName
                }),
            _ => throw new ArgumentOutOfRangeException(nameof(intent), intent, "Unsupported transactional email intent.")
        };

        var values = parameters.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        values["accent_color"] = context.AccentColor.Value;
        values["font_family"] = EmailFontFamily.Default;
        var rendered = renderer.Render(BuiltInEmailTemplateCatalog.CreateTemplate(emailType), values);
        return new RenderedTransactionalEmail(emailType, rendered.Subject, rendered.TextBody, rendered.HtmlBody);
    }

    public async ValueTask<ReconfirmationEmailCompositionScope> CreateReconfirmationScopeAsync(
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default)
    {
        var context = await LoadContextAsync(teamId, eventId, cancellationToken);
        return new ReconfirmationEmailCompositionScope(
            context,
            BuiltInEmailTemplateCatalog.CreateTemplate(BuiltInEmailTemplateNames.Reconfirmation),
            renderer);
    }

    private async ValueTask<TransactionalEmailContext> LoadContextAsync(
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken)
    {
        var projection = await readStore.EventEmailContexts.AsNoTracking().FirstOrDefaultAsync(
            c => c.TeamId == teamId && c.TicketedEventId == eventId,
            cancellationToken);
        if (projection is null || !projection.HasRequiredRenderingContext)
            throw new EventEmailContextMissingException(teamId.Value, eventId.Value);

        var team = await readStore.TeamEmailContexts.AsNoTracking().FirstOrDefaultAsync(
            c => c.TeamId == teamId,
            cancellationToken);
        var publicLink = $"{publicEventLinksOptions.Value.BaseUrl.TrimEnd('/')}/{projection.PublicSlug}";
        return new TransactionalEmailContext(
            teamId,
            eventId,
            team?.TeamName ?? "Admitto",
            team?.AccentColor ?? AccentColor.From(AccentColor.Default),
            projection.EventName!,
            projection.WebsiteUrl!,
            publicLink,
            projection.TimeZone ?? string.Empty,
            projection.ReconfirmOpensAt,
            projection.ReconfirmClosesAt,
            projection.ReconfirmMinEmailIntervalHours,
            projection.IsArchived);
    }

    /// <summary>
    /// Formats an instant in the event's time zone, culture-independently, with the IANA zone id as a label
    /// (e.g. <c>"5 September 2026, 16:30 (Europe/Amsterdam)"</c>). Falls back to UTC for a missing or unrecognized
    /// zone.
    /// </summary>
    private static string FormatWithTimeZone(DateTimeOffset instant, string timeZoneId)
    {
        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(
                string.IsNullOrWhiteSpace(timeZoneId) ? "UTC" : timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.Utc;
        }

        var local = TimeZoneInfo.ConvertTime(instant, timeZone);
        return $"{local.ToString("d MMMM yyyy, HH:mm", CultureInfo.InvariantCulture)} ({timeZone.Id})";
    }

    /// <summary>
    /// "You've reached the top of the waitlist" only holds for an automatic, front-of-queue offer; a VIP
    /// promotion skips the queue, and a capacity-opened release offers everyone at once.
    /// </summary>
    private static string WaitlistOfferIntroText(WaitlistOfferReason reason, string eventName) => reason switch
    {
        WaitlistOfferReason.VipPromotion =>
            $"Great news! You've been given a ticket offer for {eventName}.",
        WaitlistOfferReason.CapacityOpenedForEveryone =>
            $"Great news! {eventName} has opened up space for everyone on the waitlist, including you.",
        _ => $"Great news! A spot has opened up at {eventName} and you've reached the top of the waitlist."
    };

    /// <summary>
    /// What happens if the offer lapses unclaimed — only an automatic, front-of-queue offer hands the spot to a
    /// "next person"; a VIP offer skipped the queue, and a capacity-opened release already offered everyone else
    /// their own spot, so there's nobody further in line for either.
    /// </summary>
    private static string WaitlistOfferExpiryNote(WaitlistOfferReason reason) => reason switch
    {
        WaitlistOfferReason.VipPromotion => "After that, the spot may no longer be available.",
        WaitlistOfferReason.CapacityOpenedForEveryone => "After that, the spot may no longer be available.",
        _ => "After that, the spot may be offered to the next person on the waitlist."
    };

    private static Dictionary<string, object?> CancellationParameters(
        string firstName,
        TransactionalEmailContext context,
        RegistrationId registrationId) => new(StringComparer.OrdinalIgnoreCase)
        {
            ["first_name"] = firstName,
            ["event_name"] = context.EventName,
            ["register_link"] = context.GetLinks(registrationId).RegisterLink,
            ["event_website"] = context.WebsiteUrl
        };
}

internal sealed class EventEmailContextMissingException(Guid teamId, Guid eventId)
    : InvalidOperationException($"Email event context is missing required rendering fields for team '{teamId}' event '{eventId}'.");
