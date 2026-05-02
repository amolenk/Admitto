using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.Entities;

/// <summary>
/// A ticket type within a ticket catalog. Keyed by slug.
/// Combines ticket definition (name, time slots) with capacity tracking (max, used).
/// </summary>
public class TicketType : Entity<string>
{
    private TicketType() { }

    internal TicketType(
        string slug,
        DisplayName name,
        TimeSlot[] timeSlots,
        int? maxCapacity)
        : base(slug)
    {
        Name = name;
        TimeSlotSlugs = timeSlots.Select(ts => ts.Slug.Value).ToArray();
        MaxCapacity = maxCapacity;
        UsedCapacity = 0;
        IsCancelled = false;
    }

    public DisplayName Name { get; private set; }
    public string[] TimeSlotSlugs { get; private set; } = [];
    public TimeSlot[] TimeSlots => TimeSlotSlugs.Select(s => new TimeSlot(Slug.From(s))).ToArray();
    public int? MaxCapacity { get; private set; }
    public int UsedCapacity { get; private set; }
    public bool IsCancelled { get; private set; }

    public void UpdateName(DisplayName name)
    {
        Name = name;
    }

    public void UpdateCapacity(int? maxCapacity)
    {
        MaxCapacity = maxCapacity;
    }

    public void Cancel()
    {
        if (IsCancelled)
            throw new BusinessRuleViolationException(Errors.TicketTypeAlreadyCancelled(Id));

        IsCancelled = true;
    }

    /// <summary>
    /// Increments used capacity. Throws if MaxCapacity is null (not available) or if sold out.
    /// </summary>
    public void ClaimWithEnforcement()
    {
        if (MaxCapacity is null)
            throw new BusinessRuleViolationException(Errors.TicketTypeNotAvailable(Id));

        if (UsedCapacity >= MaxCapacity.Value)
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
        public static Error TicketTypeAlreadyCancelled(string slug) =>
            new("ticket_type.already_cancelled",
                "The ticket type is already cancelled.",
                Details: new Dictionary<string, object?> { ["slug"] = slug });

        public static Error TicketTypeNotAvailable(string slug) =>
            new("ticket_type.not_available",
                "Ticket type is not available for self-service registration.",
                Details: new Dictionary<string, object?> { ["slug"] = slug });

        public static Error TicketTypeAtCapacity(string slug) =>
            new("ticket_type.at_capacity",
                "Ticket type is at full capacity.",
                Details: new Dictionary<string, object?> { ["slug"] = slug });
    }
}
