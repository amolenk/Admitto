using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.Entities;

/// <summary>
/// Tracks ticket capacity for an event. Keyed by TicketedEventId.
/// Optimistic concurrency is enforced via the inherited Version row-version token.
/// </summary>
public class EventCapacity : Aggregate<TicketedEventId>
{
    private readonly List<TicketCapacity> _ticketCapacities = [];

    private EventCapacity() { }

    private EventCapacity(TicketedEventId id) : base(id) { }

    public IReadOnlyList<TicketCapacity> TicketCapacities => _ticketCapacities.AsReadOnly();

    public static EventCapacity Create(TicketedEventId eventId) => new(eventId);

    /// <summary>
    /// Adds or updates a TicketCapacity entry for the given slug.
    /// </summary>
    public void SetTicketCapacity(string slug, int? maxCapacity)
    {
        var existing = _ticketCapacities.FirstOrDefault(tc => tc.Id == slug);
        if (existing is null)
            _ticketCapacities.Add(TicketCapacity.Create(slug, maxCapacity));
        else
            existing.UpdateMaxCapacity(maxCapacity);
    }

    /// <summary>
    /// Claims tickets for the given slugs. If enforce is true, capacity is enforced (self-service path).
    /// If enforce is false, UsedCapacity is incremented without enforcement (coupon path).
    /// </summary>
    public void Claim(IReadOnlyList<string> slugs, bool enforce)
    {
        foreach (var slug in slugs)
        {
            var capacity = _ticketCapacities.FirstOrDefault(tc => tc.Id == slug);
            if (capacity is null)
                throw new BusinessRuleViolationException(Errors.TicketCapacityNotFound(slug));

            if (enforce)
                capacity.ClaimWithEnforcement();
            else
                capacity.ClaimUncapped();
        }
    }

    private static class Errors
    {
        public static Error TicketCapacityNotFound(string slug) =>
            new("ticket_capacity_not_found", "Capacity record not found for ticket type.",
                Details: new Dictionary<string, object?> { ["slug"] = slug });
    }
}