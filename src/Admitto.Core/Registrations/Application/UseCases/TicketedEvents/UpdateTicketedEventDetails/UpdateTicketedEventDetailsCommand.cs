using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.UpdateTicketedEventDetails;

internal sealed record UpdateTicketedEventDetailsCommand(
    Guid EventId,
    Guid TeamId,
    uint? ExpectedVersion,
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    string TimeZone,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    TimeOnly QuietHoursStart,
    TimeOnly QuietHoursEnd,
    string? PublicSlug = null) : Command;
