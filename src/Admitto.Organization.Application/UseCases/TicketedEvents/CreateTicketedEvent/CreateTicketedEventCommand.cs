using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Organization.Application.UseCases.TicketedEvents.CreateTicketedEvent;

internal sealed record CreateTicketedEventCommand(
    Guid TeamId,
    string Slug,
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt)
    : Command;
