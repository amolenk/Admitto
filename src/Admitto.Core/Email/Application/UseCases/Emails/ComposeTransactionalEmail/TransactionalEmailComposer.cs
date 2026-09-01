using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTransactionalEmail;

internal sealed class PublicEventLinksOptions
{
    // Public event links are owned and configured by Registrations.
    public const string SectionName = "Registrations:PublicEventLinks";
    public string BaseUrl { get; init; } = "http://localhost";
}

/// <summary>
/// Cause-specific facts for one transactional email. Recipient selection,
/// idempotency, claims, and delivery are deliberately outside this model.
/// </summary>
internal abstract record TransactionalEmailIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId);

internal sealed record TicketConfirmationIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    RegistrationId RegistrationId,
    string FirstName,
    IReadOnlyList<string> TicketTypes)
    : TransactionalEmailIntent(TeamId, TicketedEventId);

internal sealed record CouponInvitationIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string CouponCode)
    : TransactionalEmailIntent(TeamId, TicketedEventId);

internal sealed record WaitlistOfferIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string CouponCode,
    string TicketTypeName,
    DateTimeOffset ExpiresAt)
    : TransactionalEmailIntent(TeamId, TicketedEventId);

internal abstract record RegistrationCancellationIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string FirstName,
    RegistrationId RegistrationId)
    : TransactionalEmailIntent(TeamId, TicketedEventId);

internal sealed record AttendeeRequestCancellationIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string FirstName,
    RegistrationId RegistrationId)
    : RegistrationCancellationIntent(TeamId, TicketedEventId, FirstName, RegistrationId);

internal sealed record ReconfirmAutoCancellationIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string FirstName,
    RegistrationId RegistrationId)
    : RegistrationCancellationIntent(TeamId, TicketedEventId, FirstName, RegistrationId);

internal sealed record VisaLetterDeniedCancellationIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string FirstName,
    RegistrationId RegistrationId)
    : RegistrationCancellationIntent(TeamId, TicketedEventId, FirstName, RegistrationId);

internal sealed record VerificationCodeIntent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string PlainCode)
    : TransactionalEmailIntent(TeamId, TicketedEventId);

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
                BuiltInEmailTemplateNames.TicketConfirmation,
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["first_name"] = value.FirstName,
                    ["team_name"] = context.TeamName,
                    ["event_name"] = context.EventName,
                    ["public_event_link"] = context.GetLinks(value.RegistrationId).PublicEventLink,
                    ["qrcode_link"] = context.GetLinks(value.RegistrationId).QRCodeLink,
                    ["edit_registration_link"] = context.GetLinks(value.RegistrationId).EditRegistrationLink,
                    ["ticket_types"] = value.TicketTypes
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
                    ["expires_at"] = value.ExpiresAt.ToString("f"),
                    ["register_link"] = context.GetLinks(null).RegisterLink
                }),
            AttendeeRequestCancellationIntent value => (
                BuiltInEmailTemplateNames.Cancellation,
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

internal sealed record TransactionalEmailContext(
    TeamId TeamId,
    TicketedEventId EventId,
    string TeamName,
    AccentColor AccentColor,
    string EventName,
    string WebsiteUrl,
    string PublicEventLink,
    string TimeZone,
    DateTimeOffset? ReconfirmOpensAt,
    DateTimeOffset? ReconfirmClosesAt,
    int? ReconfirmMinEmailIntervalHours,
    bool IsArchived)
{
    public RegistrationEmailLinks GetLinks(RegistrationId? registrationId) =>
        RegistrationEmailLinks.From(PublicEventLink, registrationId);
}

internal sealed class EventEmailContextMissingException(Guid teamId, Guid eventId)
    : InvalidOperationException($"Email event context is missing required rendering fields for team '{teamId}' event '{eventId}'.");
