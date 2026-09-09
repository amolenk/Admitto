using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Domain.Entities;

/// <summary>
/// A ticket type within a ticket catalog. Keyed by server-generated ID.
/// Combines ticket definition (name, time slots) with capacity tracking (max, used).
/// </summary>
public class TicketType : Entity<TicketTypeId>
{
    private TicketType() { }

    internal TicketType(
        TicketTypeId id,
        TicketTypeName name,
        TimeSlot[] timeSlots,
        int? maxCapacity,
        bool selfServiceEnabled = true,
        bool waitlistEnabled = false,
        int claimWindowHours = 8,
        ReconfirmationEmailLimit? maxReconfirmationEmails = null)
        : base(id)
    {
        Name = name;
        TimeSlots = timeSlots;
        MaxCapacity = maxCapacity;
        UsedCapacity = 0;
        SelfServiceEnabled = selfServiceEnabled;
        WaitlistEnabled = waitlistEnabled;
        ClaimWindowHours = claimWindowHours;
        UpdateMaxReconfirmationEmails(maxReconfirmationEmails);
    }

    public TicketTypeName Name { get; private set; }
    public TimeSlot[] TimeSlots { get; private set; } = [];
    public int? MaxCapacity { get; private set; }
    public int UsedCapacity { get; private set; }
    public bool SelfServiceEnabled { get; private set; } = true;
    public bool WaitlistEnabled { get; private set; }
    public bool WaitlistMode { get; private set; }
    public int ClaimWindowHours { get; private set; } = 8;
    public ReconfirmationEmailLimit? MaxReconfirmationEmails { get; private set; }
    public bool IsSoldOut => MaxCapacity is not null && UsedCapacity >= MaxCapacity.Value;

    public void UpdateName(TicketTypeName name)
    {
        Name = name;
    }

    public void UpdateCapacity(int? maxCapacity)
    {
        MaxCapacity = maxCapacity;
    }

    public void UpdateSelfServiceEnabled(bool enabled)
    {
        SelfServiceEnabled = enabled;
    }

    public void UpdateMaxReconfirmationEmails(ReconfirmationEmailLimit? value)
    {
        MaxReconfirmationEmails = value;
    }

    internal void EnableWaitlist()
    {
        WaitlistEnabled = true;
    }

    internal void DisableWaitlist()
    {
        WaitlistEnabled = false;
    }

    internal void UpdateClaimWindowHours(int hours)
    {
        ClaimWindowHours = hours;
    }

    internal void ActivateWaitlistMode()
    {
        WaitlistMode = true;
    }

    internal void DeactivateWaitlistMode()
    {
        WaitlistMode = false;
    }

    /// <summary>
    /// Increments used capacity. Throws if in WaitlistMode or sold out. Self-service availability is checked upstream at catalog level.
    /// </summary>
    public void ClaimWithEnforcement()
    {
        if (WaitlistMode)
            throw new BusinessRuleViolationException(Errors.TicketTypeInWaitlistMode(Id));

        if (IsSoldOut)
            throw new BusinessRuleViolationException(Errors.TicketTypeAtCapacity(Id));

        UsedCapacity++;
    }

    /// <summary>
    /// Increments used capacity regardless of MaxCapacity. Used for coupon-based registrations.
    /// </summary>
    public void ClaimUncapped()
    {
        UsedCapacity++;
    }

    /// <summary>
    /// Decrements used capacity by 1, clamped at zero.
    /// </summary>
    public void ReleaseCapacity()
    {
        UsedCapacity = Math.Max(0, UsedCapacity - 1);
    }

    internal static class Errors
    {
        public static Error TicketTypeNotAvailable(TicketTypeId id) =>
            new("ticket_type.not_available",
                "Ticket type is not available for self-service registration.",
                Details: new Dictionary<string, object?> { ["id"] = id.Value });

        public static Error TicketTypeInWaitlistMode(TicketTypeId id) =>
            new("ticket_type.waitlist_mode",
                "This ticket type is currently in waitlist mode.",
                Details: new Dictionary<string, object?> { ["id"] = id.Value });

        public static Error TicketTypeAtCapacity(TicketTypeId id) =>
            new("ticket_type.at_capacity",
                "Ticket type is at full capacity.",
                Details: new Dictionary<string, object?> { ["id"] = id.Value });

    }
}
