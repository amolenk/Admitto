using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Contracts;

public record TicketTypeAddedModuleEvent : ModuleEvent
{
    public required Guid TicketedEventId { get; init; }
    public required string Slug { get; init; }
    public required string Name { get; init; }
    public required string[] TimeSlots { get; init; }
    public int? Capacity { get; init; }
}
