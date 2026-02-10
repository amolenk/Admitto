namespace Amolenk.Admitto.Registrations.Application.Persistence;

public class TicketCapacityRecord 
{
    public Guid TicketTypeId { get; set; }
    public int MaxCapacity { get; set; }
    public int UsedCapacity { get; set; }
}