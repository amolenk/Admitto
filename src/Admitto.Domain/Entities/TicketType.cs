using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Domain.Entities;

public class TicketType : Entity
{
    [JsonConstructor]
    private TicketType(Guid id, string sessionName, DateTime sessionStartDateTime, DateTime sessionEndDateTime, 
        int maxCapacity) : base(id)
    {
        SessionName = sessionName;
        SessionStartDateTime = sessionStartDateTime;
        SessionEndDateTime = sessionEndDateTime;
        MaxCapacity = maxCapacity;
        RemainingCapacity = maxCapacity;
    }

    public string SessionName { get; private set; }
    public DateTime SessionStartDateTime { get; private set; }
    public DateTime SessionEndDateTime { get; private set; }
    public int MaxCapacity { get; private set; }
    public int RemainingCapacity { get; private set; }

    public static TicketType Create(string sessionName, DateTime sessionStartDateTime, DateTime sessionEndDateTime, 
        int maxCapacity)
    {
        if (string.IsNullOrWhiteSpace(sessionName))
            throw new ValidationException("Session name cannot be empty.");
        
        if (sessionEndDateTime < sessionStartDateTime)
            throw new ValidationException("Session end date/time should be greater than start date/time.");

        if (maxCapacity <= 0)
            throw new ValidationException("Max capacity should be greater than 0.");

        var id = DeterministicGuidGenerator.Generate(sessionName);
        
        return new TicketType(id, sessionName, sessionStartDateTime, sessionEndDateTime, maxCapacity);
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
