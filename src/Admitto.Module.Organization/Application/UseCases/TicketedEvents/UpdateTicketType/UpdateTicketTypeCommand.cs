using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.UpdateTicketType;

internal sealed record UpdateTicketTypeCommand(
    Guid TeamId,
    Guid EventId,
    string TicketTypeSlug,
    string? Name,
    int? Capacity,
    uint? ExpectedVersion) : Command;
