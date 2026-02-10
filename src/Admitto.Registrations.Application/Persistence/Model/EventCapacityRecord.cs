namespace Amolenk.Admitto.Registrations.Application.Persistence;

public class EventCapacityRecord 
{
    public Guid EventId { get; set; }
    public List<TicketCapacityRecord> TicketCapacities { get; init; } = [];
}