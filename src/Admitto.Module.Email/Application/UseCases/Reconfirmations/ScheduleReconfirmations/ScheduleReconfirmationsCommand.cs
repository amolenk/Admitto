using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations;

/// <summary>
/// Schedules (or removes) the per-event Quartz trigger that fires the
/// reconfirm cadence job (per design D6). When <see cref="Spec"/> is
/// <c>null</c>, the per-event trigger is removed; otherwise it is upserted.
/// All operations are idempotent so they can be safely re-issued (e.g. on
/// outbox redelivery or worker startup reconciliation).
/// </summary>
internal sealed record ScheduleReconfirmationsCommand(
    TicketedEventId TicketedEventId,
    ReconfirmTriggerSpecDto? Spec) : Command;
