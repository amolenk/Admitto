using System.Text.Json.Serialization;
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
            throw new DomainRuleException(DomainRuleError.TicketType.NameIsRequired);
        
        if (maxCapacity <= 0)
            throw new DomainRuleException(DomainRuleError.TicketType.MaxCapacityMustBeGreaterThan(0));

        var id = DeterministicGuid.Create(name);
        
        return new TicketType(id, slug, name, slotName, maxCapacity);
    }

    public bool HasAvailableCapacity(int quantity)
    {
        return RemainingCapacity >= quantity;
    }
    
    public void ClaimTickets(int quantity)
    {
        UsedCapacity += quantity;
    }
    
    public void ReleaseTickets(int quantity)
    {
        UsedCapacity -= quantity;
    }
}
