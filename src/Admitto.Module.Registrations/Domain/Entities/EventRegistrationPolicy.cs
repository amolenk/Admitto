using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.Entities;

/// <summary>
/// Stores registration policy for an event: registration window, optional email domain restriction,
/// and event lifecycle status synced from the Organization module.
/// </summary>
public class EventRegistrationPolicy : Aggregate<TicketedEventId>
{
    private EventRegistrationPolicy() { }

    private EventRegistrationPolicy(TicketedEventId id) : base(id) { }

    public DateTimeOffset? RegistrationWindowOpensAt { get; private set; }
    public DateTimeOffset? RegistrationWindowClosesAt { get; private set; }
    public string? AllowedEmailDomain { get; private set; }
    public EventLifecycleStatus EventLifecycleStatus { get; private set; } = EventLifecycleStatus.Active;

    public bool IsEventActive => EventLifecycleStatus == EventLifecycleStatus.Active;

    public bool HasRegistrationWindow =>
        RegistrationWindowOpensAt.HasValue && RegistrationWindowClosesAt.HasValue;

    public static EventRegistrationPolicy Create(TicketedEventId eventId) => new(eventId);

    public void SetWindow(DateTimeOffset opensAt, DateTimeOffset closesAt)
    {
        if (closesAt <= opensAt)
            throw new BusinessRuleViolationException(Errors.WindowCloseBeforeOpen);

        RegistrationWindowOpensAt = opensAt;
        RegistrationWindowClosesAt = closesAt;
    }

    public void ClearWindow()
    {
        RegistrationWindowOpensAt = null;
        RegistrationWindowClosesAt = null;
    }

    public void SetDomainRestriction(string? domain)
    {
        AllowedEmailDomain = domain;
    }

    /// <summary>
    /// Returns true if the given time falls within the registration window.
    /// If no window is configured, returns false (registration is closed).
    /// </summary>
    public bool IsRegistrationOpen(DateTimeOffset now)
    {
        if (!HasRegistrationWindow) return false;
        return now >= RegistrationWindowOpensAt!.Value && now <= RegistrationWindowClosesAt!.Value;
    }

    /// <summary>
    /// Returns true if the email domain is allowed. Returns true when no restriction is configured.
    /// </summary>
    public bool IsEmailDomainAllowed(string email)
    {
        if (AllowedEmailDomain is null) return true;
        return email.EndsWith(AllowedEmailDomain, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sets the lifecycle status to Cancelled. Idempotent — no-op if already Cancelled.
    /// </summary>
    public void SetCancelled()
    {
        if (EventLifecycleStatus != EventLifecycleStatus.Cancelled)
            EventLifecycleStatus = EventLifecycleStatus.Cancelled;
    }

    /// <summary>
    /// Sets the lifecycle status to Archived. Idempotent — no-op if already Archived.
    /// </summary>
    public void SetArchived()
    {
        if (EventLifecycleStatus != EventLifecycleStatus.Archived)
            EventLifecycleStatus = EventLifecycleStatus.Archived;
    }

    internal static class Errors
    {
        public static readonly Error WindowCloseBeforeOpen = new(
            "registration_policy.window_close_before_open",
            "Registration window close time must be after open time.");
    }
}
