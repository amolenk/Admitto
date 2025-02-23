using System.ComponentModel.DataAnnotations;

namespace Amolenk.Admitto.Domain.ValueObjects;

/// <summary>
/// Represents an order of tickets placed by an attendee.
/// </summary>
public class TicketOrder
{
    public TicketOrder(IEnumerable<Guid> ticketTypeIds)
    {
        var uniqueIds = new HashSet<Guid>(ticketTypeIds);

        if (uniqueIds.Count == 0)
            throw new ValidationException("At least one ticket type must be selected.");

        TicketTypeIds = uniqueIds.ToList().AsReadOnly();
    }
    
    public IReadOnlyCollection<Guid> TicketTypeIds { get; }
}