using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.ModuleEvents;

/// <summary>
/// Published after a <see cref="Domain.Entities.BulkEmailJob"/> has been created
/// (and committed via the outbox). The Worker host's handler picks this up and
/// schedules a one-shot Quartz trigger that drives the fan-out.
/// </summary>
public sealed record BulkEmailJobRequestedModuleEvent : ModuleEvent
{
    public required Guid BulkEmailJobId { get; init; }
    public required Guid TeamId { get; init; }
    public required Guid TicketedEventId { get; init; }
}
