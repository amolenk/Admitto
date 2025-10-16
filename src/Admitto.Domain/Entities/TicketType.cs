using System.Text.Json.Serialization;
using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Domain.Entities;

public class TicketType : Entity
{
    [JsonConstructor]
    private TicketType(Guid id, string slug, string name, List<string> slotNames, int maxCapacity) : base(id)
    {
        Slug = slug;
        Name = name;
        SlotNames = slotNames;
        MaxCapacity = maxCapacity;
        UsedCapacity = 0;
    }

    public string Slug { get; private set; }
    public string Name { get; private set; }
    public List<string> SlotNames { get; private set; } = [];
    public int MaxCapacity { get; private set; }
    public int UsedCapacity { get; private set; }
    
    private int RemainingCapacity => MaxCapacity - UsedCapacity;

    public static TicketType Create(string slug, string name, string slotName, int maxCapacity)
    {
        return Create(slug, name, new List<string> { slotName }, maxCapacity);
    }

    public static TicketType Create(string slug, string name, List<string> slotNames, int maxCapacity)
    {
        EnsureValidMaxCapacity(maxCapacity);

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainRuleException(DomainRuleError.TicketType.NameIsRequired);
        
        if (slotNames == null || slotNames.Count == 0)
            throw new DomainRuleException(DomainRuleError.TicketType.SlotNamesAreRequired);
 
        var id = DeterministicGuid.Create(name);
        
        return new TicketType(id, slug, name, slotNames, maxCapacity);
    }

    public bool HasAvailableCapacity(int quantity = 1)
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
    
    public void UpdateMaxCapacity(int maxCapacity)
    {
        EnsureValidMaxCapacity(maxCapacity);
        MaxCapacity = maxCapacity;
    }
    
    private static void EnsureValidMaxCapacity(int maxCapacity)
    {
        // We do allow ticket types with maxCapacity of 0, which means no tickets can be sold.
        // We also don't care about the current UsedCapacity. It may be useful to reduce the capacity at some point
        // so that cancelled tickets cannot be re-sold.
        if (maxCapacity < 0)
        {
            throw new DomainRuleException(DomainRuleError.TicketType.MaxCapacityMustBeZeroOrPositive);
        }
    }
}
