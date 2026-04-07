using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.Entities;

/// <summary>
/// Owns the ticket types for an event. Keyed by TicketedEventId.
/// Combines ticket type definition with capacity tracking in a single aggregate.
/// </summary>
public class TicketCatalog : Aggregate<TicketedEventId>
{
    private readonly List<TicketType> _ticketTypes = [];

    private TicketCatalog() { }

    private TicketCatalog(TicketedEventId id) : base(id) { }

    public IReadOnlyList<TicketType> TicketTypes => _ticketTypes.AsReadOnly();

    public static TicketCatalog Create(TicketedEventId eventId) => new(eventId);

    public void AddTicketType(
        Slug slug,
        DisplayName name,
        TimeSlot[] timeSlots,
        int? maxCapacity)
    {
        if (_ticketTypes.Any(tt => tt.Id == slug.Value))
            throw new BusinessRuleViolationException(Errors.DuplicateTicketTypeSlug(slug));

        _ticketTypes.Add(new TicketType(slug.Value, name, timeSlots, maxCapacity));
    }

    public void UpdateTicketType(
        Slug slug,
        DisplayName? name,
        int? maxCapacity)
    {
        var ticketType = FindTicketType(slug);

        if (ticketType.IsCancelled)
            throw new BusinessRuleViolationException(Errors.TicketTypeAlreadyCancelled(slug));

        if (name is not null)
            ticketType.UpdateName(name.Value);

        ticketType.UpdateCapacity(maxCapacity);
    }

    public void CancelTicketType(Slug slug)
    {
        var ticketType = FindTicketType(slug);
        ticketType.Cancel();
    }

    public TicketType? GetTicketType(string slug)
    {
        return _ticketTypes.FirstOrDefault(tt => tt.Id == slug);
    }

    /// <summary>
    /// Claims tickets for the given slugs. If enforce is true, capacity is enforced (self-service path).
    /// If enforce is false, UsedCapacity is incremented without enforcement (coupon path).
    /// </summary>
    public void Claim(IReadOnlyList<string> slugs, bool enforce)
    {
        foreach (var slug in slugs)
        {
            var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == slug);
            if (ticketType is null)
                throw new BusinessRuleViolationException(Errors.TicketTypeNotFound(slug));

            if (enforce)
                ticketType.ClaimWithEnforcement();
            else
                ticketType.ClaimUncapped();
        }
    }

    private TicketType FindTicketType(Slug slug)
    {
        var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Id == slug.Value);
        if (ticketType is null)
            throw new BusinessRuleViolationException(Errors.TicketTypeNotFound(slug.Value));

        return ticketType;
    }

    internal static class Errors
    {
        public static Error DuplicateTicketTypeSlug(Slug slug) =>
            new("ticket_catalog.duplicate_slug",
                "A ticket type with this slug already exists.",
                Details: new Dictionary<string, object?> { ["slug"] = slug.Value });

        public static Error TicketTypeNotFound(string slug) =>
            new("ticket_catalog.ticket_type_not_found",
                "Ticket type could not be found.",
                Type: ErrorType.NotFound,
                Details: new Dictionary<string, object?> { ["slug"] = slug });

        public static Error TicketTypeAlreadyCancelled(Slug slug) =>
            new("ticket_catalog.ticket_type_already_cancelled",
                "The ticket type is already cancelled.",
                Details: new Dictionary<string, object?> { ["slug"] = slug.Value });
    }
}
