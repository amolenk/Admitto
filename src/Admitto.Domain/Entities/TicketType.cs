using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Amolenk.Admitto.Domain.Exceptions;
using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Domain.Entities;

public class TicketType : Entity
{
    [JsonConstructor]
    private TicketType(Guid id, string slug, string name, string slotName, int maxCapacity) : base(id)
    {
        Slug = slug;
        Name = name;
        SlotName = slotName;
        MaxCapacity = maxCapacity;
        UsedCapacity = 0;
    }

    public string Slug { get; private set; }
    public string Name { get; private set; }
    public string SlotName { get; private set; }
    public int MaxCapacity { get; private set; }
    public int UsedCapacity { get; private set; }
    
    private int RemainingCapacity => MaxCapacity - UsedCapacity;

    public static TicketType Create(string slug, string name, string slotName, int maxCapacity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleException("Name cannot be empty.");
        
        if (maxCapacity <= 0)
            throw new BusinessRuleException("Max capacity should be greater than 0.");

        var id = DeterministicGuid.Create(name);
        
        return new TicketType(id, slug, name, slotName, maxCapacity);
    }

    public bool HasAvailableCapacity(int quantity)
    {
        return RemainingCapacity >= quantity;
    }
    
    public bool TryReserveTickets(int quantity, bool ignoreAvailability)
    {
        if (!ignoreAvailability && RemainingCapacity < quantity)
        {
            return false;
        }
        
        UsedCapacity += quantity;
        return true;
    }
}
