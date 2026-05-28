using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.MaterializeTicketedEvent;

internal sealed record MaterializeTicketedEventCommand(
    Guid CreationRequestId,
    Guid TeamId,
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZone) : Command;
