using System.Text.Json.Serialization;

namespace Amolenk.Admitto.Domain.ValueObjects;

/// <summary>
/// Represents the quantity of a specific type of ticket in a registration.
/// </summary>
public class TicketQuantity
{
    [JsonConstructor]
    private TicketQuantity(string slug, int quantity)
    {
        Slug = slug;
        Quantity = quantity;
    }
    
    public static TicketQuantity Create(string slug, int quantity)
    {
        if (quantity <= 0)
        {
            throw DomainError.TicketType.QuantityMustBeGreaterThanZero();
        }

        return new TicketQuantity(slug, quantity);
    }
    
    public string Slug { get; private set; }
    public int Quantity { get; private set; }
}