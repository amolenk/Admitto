namespace Amolenk.Admitto.Domain.ValueObjects;

public record TicketTypeId(Guid Value)
{
    public static implicit operator TicketTypeId(Guid value) => new(value);
    
    public static implicit operator Guid(TicketTypeId ticketTypeId) => ticketTypeId.Value;
}