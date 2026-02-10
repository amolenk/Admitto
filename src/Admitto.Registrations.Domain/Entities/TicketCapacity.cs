using System.Text.Json.Serialization;
using Amolenk.Admitto.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.Entities;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Registrations.Domain.Entities;

public class TicketCapacity : Entity<TicketTypeId>
{
    [JsonConstructor]
    private TicketCapacity(TicketTypeId id, string name, int maxCapacity, int usedCapacity) : base(id)
    {
        Name = name;
        MaxCapacity = maxCapacity;
        UsedCapacity = 0;
    }

    public string Name { get; private set; }
    public int MaxCapacity { get; private set; }
    public int UsedCapacity { get; private set; }
    
    private int RemainingCapacity => MaxCapacity - UsedCapacity;

    public static TicketCapacity Create(TicketTypeId id, string name, int maxCapacity, int usedCapacity)
    {
//        EnsureValidMaxCapacity(maxCapacity);

        // if (string.IsNullOrWhiteSpace(name))
        //     throw new DomainRuleException(DomainRuleError.TicketType.NameIsRequired);
        //
        // if (slotNames == null || slotNames.Count == 0)
        //     throw new DomainRuleException(DomainRuleError.TicketType.SlotNamesAreRequired);
        //
        // var id = DeterministicGuid.Create(name);
        
        return new TicketCapacity(id, name, maxCapacity, usedCapacity);
    }

    public static TicketCapacity Rehydrate(
        TicketTypeId ticketTypeId,
        int maxCapacity,
        int usedCapacity)
    {
        throw new NotImplementedException();
    }

    public bool HasAvailableCapacity(int quantity = 1)
    {
        return RemainingCapacity >= quantity;
    }
    
    public void ReleaseTickets(int quantity)
    {
        UsedCapacity -= quantity;
    }
    
    public void UpdateMaxCapacity(int maxCapacity)
    {
        // EnsureValidMaxCapacity(maxCapacity);
        MaxCapacity = maxCapacity;
    }
    
    // private static void EnsureValidMaxCapacity(int maxCapacity)
    // {
    //     // We do allow ticket types with maxCapacity of 0, which means no tickets can be sold.
    //     // We also don't care about the current UsedCapacity. It may be useful to reduce the capacity at some point
    //     // so that canceled tickets cannot be re-sold.
    //     if (maxCapacity < 0)
    //     {
    //         throw new DomainRuleException(DomainRuleError.TicketType.MaxCapacityMustBeZeroOrPositive);
    //     }
    // }
    public void ClaimTicket()
    {
        if (RemainingCapacity > 0)
        {
            UsedCapacity += 1;
        }
        else
        {
            throw new BusinessRuleViolationException(Errors.TicketTypeSoldOut(Id));
        }        
    }
    
    private static class Errors
    {
        public static Error TicketTypeSoldOut(TicketTypeId ticketTypeId) =>
            new(
                "ticket_type_sold_out",
                "The requested ticket type is sold out.",
                Details: new Dictionary<string, object?> { ["ticketTypeId"] = ticketTypeId.Value });
    }
}
