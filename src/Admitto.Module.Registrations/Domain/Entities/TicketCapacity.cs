using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Registrations.Domain.Entities;

/// <summary>
/// Tracks capacity for a single ticket type within an event.
/// Keyed by slug. MaxCapacity is nullable; null means the ticket type is not available for self-service registration.
/// </summary>
public class TicketCapacity : Entity<string>
{
    private TicketCapacity() { }

    private TicketCapacity(string slug, int? maxCapacity, int usedCapacity) : base(slug)
    {
        MaxCapacity = maxCapacity;
        UsedCapacity = usedCapacity;
    }

    public int? MaxCapacity { get; private set; }
    public int UsedCapacity { get; private set; }

    public static TicketCapacity Create(string slug, int? maxCapacity) =>
        new(slug, maxCapacity, 0);

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

    public void UpdateMaxCapacity(int? maxCapacity)
    {
        MaxCapacity = maxCapacity;
    }

    internal static class Errors
    {
        public static Error TicketTypeNotAvailable(string slug) =>
            new("ticket_type_not_available", "Ticket type is not available for self-service registration.",
                Details: new Dictionary<string, object?> { ["slug"] = slug });

        public static Error TicketTypeAtCapacity(string slug) =>
            new("ticket_type_at_capacity", "Ticket type is at full capacity.",
                Details: new Dictionary<string, object?> { ["slug"] = slug });
    }
}