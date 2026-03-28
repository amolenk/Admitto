using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.ModuleEvents;

public record CouponCreatedModuleEvent : ModuleEvent
{
    public required Guid CouponId { get; init; }

    public required Guid TicketedEventId { get; init; }

    public required string Email { get; init; }
}
