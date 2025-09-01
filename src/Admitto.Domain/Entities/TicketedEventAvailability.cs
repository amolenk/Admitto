using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents an event for which attendees can register.
/// </summary>
public class TicketedEventAvailability : Aggregate
{
    private readonly List<TicketType> _ticketTypes = [];
    
    // EF Core constructor
    private TicketedEventAvailability()
    {
    }

    private TicketedEventAvailability(
        Guid id,
        Guid eventId,
        DateTimeOffset registrationStartTime,
        DateTimeOffset registrationEndTime,
        string? emailDomainName)
        : base(id)
    {
        TicketedEventId = eventId;
        RegistrationStartTime = registrationStartTime;
        RegistrationEndTime = registrationEndTime;
        EmailDomainName = emailDomainName;
    }

    public Guid TicketedEventId { get; private set; }
    public DateTimeOffset RegistrationStartTime { get; private set; }
    public DateTimeOffset RegistrationEndTime { get; private set; }
    public string? EmailDomainName { get; private set; }
    public IReadOnlyCollection<TicketType> TicketTypes => _ticketTypes.AsReadOnly();
    
    public static TicketedEventAvailability Create(
        Guid eventId,
        DateTimeOffset registrationStartTime,
        DateTimeOffset registrationEndTime,
        string? emailDomainName = null)
    {
        return new TicketedEventAvailability(
            Guid.NewGuid(),
            eventId,
            registrationStartTime,
            registrationEndTime,
            emailDomainName);
    }

    public void AddTicketType(string slug, string name, string slotName, int maxCapacity)
    {
        if (_ticketTypes.Any(t => t.Slug == slug))
        {
            throw new DomainRuleException(DomainRuleError.TicketedEvent.TicketTypeAlreadyExists);
        }

        var ticketType = TicketType.Create(slug, name, slotName, maxCapacity);
        _ticketTypes.Add(ticketType);
    }

    public void ClaimTickets(
        Guid ticketedEventId,
        Guid participantId,
        string email,
        string firstName,
        string lastName,
        IList<AdditionalDetail> additionalDetails,
        IList<TicketSelection> tickets, 
        bool ignoreCapacity = false)
    {
        if (tickets.Count == 0)
        {
            throw new DomainRuleException(DomainRuleError.TicketedEvent.TicketsAreRequired);
        }

        // TODO Check ticket overlaps
        
        foreach (var ticketSelection in tickets)
        {
            var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Slug == ticketSelection.TicketTypeSlug);
            if (ticketType is null)
            {
                throw new DomainRuleException(
                    DomainRuleError.TicketedEvent.InvalidTicketType(ticketSelection.TicketTypeSlug));
            }
            
            // Ensure that there's enough capacity for the requested tickets.
            if (!ignoreCapacity && !ticketType.HasAvailableCapacity(ticketSelection.Quantity))
            {
                throw new DomainRuleException(DomainRuleError.TicketedEvent.CapacityExceeded(ticketType.Slug));
            }
            
            ticketType.ClaimTickets(ticketSelection.Quantity);
        }
        
        AddDomainEvent(new TicketsClaimedDomainEvent(
            ticketedEventId,
            participantId,
            email,
            firstName,
            lastName,
            additionalDetails,
            tickets));
    }
    
    public void ReleaseTickets(IList<TicketSelection> tickets)
    {
        foreach (var ticketSelection in tickets)
        {
            var ticketType = _ticketTypes.FirstOrDefault(tt => tt.Slug == ticketSelection.TicketTypeSlug);
            ticketType!.ReleaseTickets(ticketSelection.Quantity);
        }
    }
}