using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Amolenk.Admitto.Domain.ValueObjects;

/// <summary>
/// Represents an order of tickets placed by an attendee.
/// </summary>
public class TicketOrder
{
    private readonly List<Guid> _ticketTypeIds;
    
    private TicketOrder(List<Guid> ticketTypeIds)
    {
        _ticketTypeIds = ticketTypeIds;
    }

    [JsonConstructor]
    private TicketOrder(IReadOnlyCollection<Guid> ticketTypeIds)
    {
        _ticketTypeIds = ticketTypeIds.ToList();
    }
    
    public IReadOnlyCollection<Guid> TicketTypeIds => _ticketTypeIds.AsReadOnly();
    
    public static TicketOrder Create(IEnumerable<Guid> ticketTypeIds)
    {
        var uniqueIds = new HashSet<Guid>(ticketTypeIds);

        if (uniqueIds.Count == 0)
            throw new ValidationException("At least one ticket type must be selected.");

        return new TicketOrder(uniqueIds.ToList());
    }
}