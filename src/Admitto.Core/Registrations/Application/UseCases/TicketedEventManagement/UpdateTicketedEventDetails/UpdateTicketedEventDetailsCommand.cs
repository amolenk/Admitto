using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventDetails;

internal sealed record UpdateTicketedEventDetailsCommand(
    Guid EventId,
    uint? ExpectedVersion,
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    TimeOnly QuietHoursStart,
    TimeOnly QuietHoursEnd) : Command;
