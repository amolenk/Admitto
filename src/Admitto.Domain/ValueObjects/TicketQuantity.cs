using System.Text.Json.Serialization;
using Amolenk.Admitto.Domain.Exceptions;

namespace Amolenk.Admitto.Domain.ValueObjects;

/// <summary>
/// Represents the quantity of a specific type of ticket in a registration.
/// </summary>
public class TicketQuantity
{
    public TicketQuantity(TicketTypeId ticketTypeId, int quantity)
    {
        if (quantity <= 0)
        {
            throw DomainError.TicketType.QuantityMustBeGreaterThanZero();
        }

        TicketTypeId = ticketTypeId;
        Quantity = quantity;
    }
    
    [JsonConstructor]
    private TicketQuantity(Guid ticketTypeId, int quantity)
    {
        TicketTypeId = ticketTypeId;
        Quantity = quantity;
    }
    
    public TicketTypeId TicketTypeId { get; private set; }
    public int Quantity { get; private set; }
}