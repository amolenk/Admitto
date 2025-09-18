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
        // Note: we do allow ticket types with maxCapacity of 0, which means no tickets can be sold.
        // This may be useful in some scenarios where registrations are only allowed at some later time.
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
}
