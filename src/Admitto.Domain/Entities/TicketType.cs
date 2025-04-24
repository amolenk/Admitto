using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Amolenk.Admitto.Domain.Exceptions;
using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Domain.Entities;

public class TicketType : Entity
{
    [JsonConstructor]
    private TicketType(Guid id, string name, string slotName, int maxCapacity) : base(id)
    {
        Name = name;
        SlotName = slotName;
        MaxCapacity = maxCapacity;
        RemainingCapacity = maxCapacity;
    }

    public string Name { get; private set; }
    public string SlotName { get; private set; }
    public int MaxCapacity { get; private set; }
    public int RemainingCapacity { get; private set; }

    public static TicketType Create(string name, string slotName, int maxCapacity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name cannot be empty.");
        
        if (maxCapacity <= 0)
            throw new DomainException("Max capacity should be greater than 0.");

        var id = DeterministicGuidGenerator.Generate(name);
        
        return new TicketType(id, name, slotName, maxCapacity);
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
