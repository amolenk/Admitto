using Amolenk.Admitto.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Registrations.Application.Persistence;

public class TicketRecord
{
    public Guid TicketTypeId { get; private set; }
    
    public static TicketRecord FromDomain(Ticket ticket) => new()
    {
        TicketTypeId = ticket.TicketTypeId.Value
    };
    
    public void ApplyFromDomain(Ticket ticket)
    {
        TicketTypeId = ticket.TicketTypeId.Value;
    }
    
    public Ticket ToDomain() => new(new TicketTypeId(TicketTypeId));
}
