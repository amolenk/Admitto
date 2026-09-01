using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;

/// <summary>
/// Immutable event-scoped Email context. The team and event projections are read
/// once to create this scope and all messages composed from it reuse the same
/// branding, team label, event facts, and public-event base link.
/// </summary>
internal sealed record EventEmailRenderingScope(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
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
    public RegistrationEmailLinks GetRegistrationLinks(RegistrationId registrationId) =>
        RegistrationEmailLinks.From(PublicEventLink, registrationId);

    public EventEmailContextDto ToContext(RegistrationId? registrationId)
    {
        var links = registrationId.HasValue
            ? RegistrationEmailLinks.From(PublicEventLink, registrationId)
            : RegistrationEmailLinks.From(PublicEventLink, null);

        return new EventEmailContextDto(
            TeamId.Value,
            TicketedEventId.Value,
            TeamName,
            EventName,
            WebsiteUrl,
            links.PublicEventLink,
            links.RegisterLink,
            links.QRCodeLink,
            links.CancelLink,
            links.EditRegistrationLink,
            TimeZone,
            ReconfirmOpensAt,
            ReconfirmClosesAt,
            ReconfirmMinEmailIntervalHours,
            IsArchived);
    }

}
