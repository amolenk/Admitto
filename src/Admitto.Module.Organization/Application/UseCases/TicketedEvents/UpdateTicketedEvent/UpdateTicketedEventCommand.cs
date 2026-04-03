using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.UpdateTicketedEvent;

internal sealed record UpdateTicketedEventCommand(
    Guid TeamId,
    Guid EventId,
    string? Name,
    string? WebsiteUrl,
    string? BaseUrl,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    uint? ExpectedVersion) : Command;
