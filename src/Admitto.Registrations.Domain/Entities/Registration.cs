using System.Diagnostics.CodeAnalysis;
using Amolenk.Admitto.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.Entities;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Registrations.Domain.Entities;

/// <summary>
/// Represents a registration for a ticketed event.
/// </summary>
public class Registration : Aggregate<RegistrationId>
{
    private readonly List<Ticket> _tickets = null!;
    
    // EF Core constructor
    private Registration()
    {
    }

    private Registration(
        RegistrationId id,
        TicketedEventId eventId,
        EmailAddress email)
        : base(id)
    {
        EventId = eventId;
        Email = email;

        _tickets = [];
    }

    public TicketedEventId EventId { get; private set; }
    public EmailAddress Email { get; private set; }
    public IReadOnlyList<Ticket> Tickets => _tickets.AsReadOnly();

    public static Registration Create(
        TicketedEventId eventId,
        EmailAddress email)
    {
        return new Registration(
            RegistrationId.New(),
            eventId,
            email);
    }

    public void GrantTickets(
        IReadOnlyList<TicketRequest> ticketRequests,
        IReadOnlyList<TicketTypeSnapshot> ticketTypes)
    {
        var ticketTypesDict = ticketTypes.ToDictionary(ticketTypeSnapshot => ticketTypeSnapshot.Id);

        
        
        var ticketTypeIds = ticketRequests
            .Select(tr => tr.TicketTypeId)
            .ToList();

        EnsureNoDuplicateTicketTypes(ticketTypeIds);
        EnsureNoUnknownTicketTypes(ticketTypeIds, ticketTypesDict);
        EnsureNoOverlappingTicketTypes(ticketTypeIds, ticketTypesDict);
        
        // Grant tickets
        var grantedTickets = new List<Ticket>();
        
        foreach (var (ticketTypeId, grantMode, _) in ticketRequests)
        {
            var ticketType = ticketTypesDict[ticketTypeId];
            
            // TODO Check grant mode
            
            if (_tickets.Any(t => t.TicketTypeId == ticketTypeId))
            {
                throw new BusinessRuleViolationException(Errors.TicketTypeAlreadyGranted(ticketTypeId));
            }
            
            grantedTickets.Add(new Ticket(ticketTypeId));
        }
        
        _tickets.AddRange(grantedTickets);
    }
    
    private static void EnsureNoDuplicateTicketTypes(IReadOnlyList<TicketTypeId> ticketTypeIds)
    {
        var duplicateTicketTypeIds = ticketTypeIds
            .GroupBy(id => id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        if (duplicateTicketTypeIds.Length > 0)
        {
            throw new BusinessRuleViolationException(Errors.DuplicateTicketTypes(duplicateTicketTypeIds));
        }
    }
    
    private static void EnsureNoUnknownTicketTypes(
        IReadOnlyList<TicketTypeId> ticketTypeIds,
        IReadOnlyDictionary<TicketTypeId, TicketTypeSnapshot> ticketTypes)
    {
        var unknownTicketTypeIds = ticketTypeIds
            .Where(id => !ticketTypes.ContainsKey(id))
            .Distinct()
            .ToArray();

        if (unknownTicketTypeIds.Length > 0)
        {
            throw new BusinessRuleViolationException(Errors.UnknownTicketTypes(unknownTicketTypeIds));
        }
    }

    private void EnsureNoOverlappingTicketTypes(
        IReadOnlyList<TicketTypeId> ticketTypeIds,
        IReadOnlyDictionary<TicketTypeId, TicketTypeSnapshot> ticketTypes)
    {
        // Also consider already granted ticket types
        var allTicketTypeIds = _tickets
            .Select(t => t.TicketTypeId)
            .Concat(ticketTypeIds)
            .Distinct()
            .ToList();

        var ticketTypesToCheck = allTicketTypeIds
            .Where(ticketTypes.ContainsKey)
            .Select(id => ticketTypes[id])
            .ToList();
        
        var overlappingTicketTypeIds = ticketTypesToCheck
            .SelectMany(tt => tt.TimeSlots.Select(ts => new { tt.Id, TimeSlot = ts  }))
            .GroupBy(x => x.TimeSlot)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Select(x => x.Id))
            .Distinct()
            .ToArray();

        if (overlappingTicketTypeIds.Length > 0)
        {
            throw new BusinessRuleViolationException(Errors.OverlappingTicketTypeTimeSlots(overlappingTicketTypeIds));
        }
    }
    
    internal static class Errors
    {
        public static Error UnknownTicketTypes(TicketTypeId[] ids) =>
            new(
                "ticket_types_unknown",
                "One or more ticket types are unknown.",
                Details: new Dictionary<string, object?> { ["ticketTypeIds"] = ids });

        public static Error DuplicateTicketTypes(TicketTypeId[] ids) =>
            new(
                "duplicate_ticket_types",
                "One or more ticket types are duplicates.",
                Details: new Dictionary<string, object?> { ["ticketTypeIds"] = ids });

        public static Error TicketTypeAlreadyGranted(TicketTypeId id) =>
            new(
                "duplicate_ticket_type",
                "The same ticket type has already been granted.",
                Details: new Dictionary<string, object?> { ["ticketTypeId"] = id });
    
        public static Error OverlappingTicketTypeTimeSlots(TicketTypeId[] ids) =>
            new(
                "overlapping_ticket_type_time_slots",
                "One or more ticket types have overlapping time slots.",
                Details: new Dictionary<string, object?> { ["ticketTypeIds"] = ids });
    }
}