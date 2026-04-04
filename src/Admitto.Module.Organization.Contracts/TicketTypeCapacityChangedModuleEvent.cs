using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Contracts;

public record TicketTypeCapacityChangedModuleEvent : ModuleEvent
{
    public required Guid TicketedEventId { get; init; }
    public required string Slug { get; init; }
    public int? Capacity { get; init; }
}
