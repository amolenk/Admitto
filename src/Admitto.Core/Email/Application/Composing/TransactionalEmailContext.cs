using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.Composing;

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
