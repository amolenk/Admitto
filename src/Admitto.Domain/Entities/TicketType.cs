using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Domain.Entities;

public class TicketType : Entity
{
    [JsonConstructor]
    private TicketType(Guid id, string name, DateTime startDateTime, DateTime endDateTime, int maxCapacity) : base(id)
    {
        Name = name;
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
        MaxCapacity = maxCapacity;
        RemainingCapacity = maxCapacity;
    }

    public string Name { get; private set; }
    public DateTime StartDateTime { get; private set; }
    public DateTime EndDateTime { get; private set; }
    public int MaxCapacity { get; private set; }
    public int RemainingCapacity { get; private set; }

    public static TicketType Create(string name, DateTime startDateTime, DateTime endDateTime, int maxCapacity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Name cannot be empty.");
        
        if (endDateTime < startDateTime)
            throw new ValidationException("End date/time should be greater than start date/time.");

        if (maxCapacity <= 0)
            throw new ValidationException("Max capacity should be greater than 0.");

        var id = DeterministicGuidGenerator.Generate(name);
        
        return new TicketType(id, name, startDateTime, endDateTime, maxCapacity);
    }

    public bool HasAvailableCapacity()
    {
        return RemainingCapacity > 0;
    }
    
    public void ReserveTicket()
    {
        if (RemainingCapacity == 0)
            throw new ValidationException($"No tickets available.");
        
        RemainingCapacity -= 1;
    }

    public void CancelTicket()
    {
        RemainingCapacity += 1;
    }

    public bool HasTicketsReserved()
    {
        return RemainingCapacity < MaxCapacity;
    }
}
