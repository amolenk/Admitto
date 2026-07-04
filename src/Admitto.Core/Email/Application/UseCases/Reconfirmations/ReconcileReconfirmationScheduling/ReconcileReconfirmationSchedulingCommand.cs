using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ReconcileReconfirmationScheduling;

/// <summary>
/// Reconciles the per-event reconfirm Quartz triggers with the current set of
/// active ticketed events that have a reconfirm policy. Idempotent re-issue
/// of <see cref="ScheduleReconfirmations.ScheduleReconfirmationsCommand"/> for
/// every active spec, so it is safe to invoke at worker startup.
/// </summary>
internal sealed record ReconcileReconfirmationSchedulingCommand : Command;
