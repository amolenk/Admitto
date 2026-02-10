using Amolenk.Admitto.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.Entities;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Registrations.Domain.Entities;

public class EventCapacity : Aggregate<TicketedEventId>
{
    private readonly List<TicketCapacity> _ticketTypeCapacities = [];

    // EF Core constructor
    private EventCapacity()
    {
    }

    private EventCapacity(
        TicketedEventId id)
        : base(id)
    {
    }

    public IReadOnlyCollection<TicketCapacity> TicketCapacities => _ticketTypeCapacities.AsReadOnly();

    public static EventCapacity Create(
        TicketedEventId ticketedEventId)
    {
        return new EventCapacity(ticketedEventId);
    }
    
    public static EventCapacity Rehydrate(TicketedEventId eventId, IReadOnlyList<TicketCapacity> ticketCapacities)
    {
        throw new NotImplementedException();
    }


    public void Claim(IReadOnlyList<TicketRequest> ticketRequests)
    {
        // for each ticket request, check if there is enough capacity
        // if there is enough capacity, reduce the available capacity
        // if there is not enough capacity, return an error

        foreach (var ticketRequest in ticketRequests)
        {
            var capacity = _ticketTypeCapacities
                .FirstOrDefault(tc => tc.Id == ticketRequest.TicketTypeId);
            if (capacity is null)
            {
                throw new BusinessRuleViolationException(Errors.TicketTypeCapacityNotFound(ticketRequest.TicketTypeId));
            }

            capacity.ClaimTicket();
        }
    }

    public void SetTicketTypeCapacity(TicketTypeId id, int capacity)
    {
        // TODO Validate and remove name
        _ticketTypeCapacities.Add(TicketCapacity.Create(id, "unused", capacity, 0));
    }

    private static class Errors
    {
        public static Error TicketTypeCapacityNotFound(TicketTypeId ticketTypeId) =>
            new(
                "ticket_capacity_not_found",
                "Cannot find capacity details for ticket type.",
                ErrorType.Validation,
                new Dictionary<string, object?> { ["ticketTypeId"] = ticketTypeId.Value });
    }
}