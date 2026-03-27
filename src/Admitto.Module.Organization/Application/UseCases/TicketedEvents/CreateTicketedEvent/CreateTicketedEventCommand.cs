using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CreateTicketedEvent;

internal sealed record CreateTicketedEventCommand(
    Guid TeamId,
    string Slug,
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt)
    : Command;
