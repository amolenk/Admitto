using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.Entities;

/// <summary>
/// Per-event aggregate in the Registrations module that tracks lifecycle status and acts as the
/// strong-consistency synchronization point for all policy mutations.
/// </summary>
public class TicketedEventLifecycleGuard : Aggregate<TicketedEventId>
{
    private TicketedEventLifecycleGuard() { }

    private TicketedEventLifecycleGuard(TicketedEventId id) : base(id) { }

    public EventLifecycleStatus LifecycleStatus { get; private set; } = EventLifecycleStatus.Active;
    public long PolicyMutationCount { get; private set; }

    public bool IsActive => LifecycleStatus == EventLifecycleStatus.Active;

    public static TicketedEventLifecycleGuard Create(TicketedEventId eventId) => new(eventId);

    /// <summary>
    /// Asserts that the event is Active and increments the mutation count.
    /// Called by every policy-mutation command in the same unit of work.
    /// </summary>
    public void AssertActiveAndRegisterPolicyMutation()
    {
        if (!IsActive)
            throw new BusinessRuleViolationException(Errors.EventNotActive);

        PolicyMutationCount++;
    }

    /// <summary>
    /// Sets the lifecycle status to Cancelled. Idempotent — no-op if already Cancelled.
    /// Bumps <see cref="PolicyMutationCount"/> only on a real transition.
    /// </summary>
    public void SetCancelled()
    {
        if (LifecycleStatus != EventLifecycleStatus.Cancelled)
        {
            LifecycleStatus = EventLifecycleStatus.Cancelled;
            PolicyMutationCount++;
        }
    }

    /// <summary>
    /// Sets the lifecycle status to Archived. Idempotent — no-op if already Archived.
    /// Bumps <see cref="PolicyMutationCount"/> only on a real transition.
    /// </summary>
    public void SetArchived()
    {
        if (LifecycleStatus != EventLifecycleStatus.Archived)
        {
            LifecycleStatus = EventLifecycleStatus.Archived;
            PolicyMutationCount++;
        }
    }

    internal static class Errors
    {
        public static readonly Error EventNotActive = new(
            "lifecycle_guard.event_not_active",
            "Cannot modify policies for a cancelled or archived event.",
            Type: ErrorType.Validation);
    }
}
