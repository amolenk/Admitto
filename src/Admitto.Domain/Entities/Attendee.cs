using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents a registration for a ticketed event.
/// </summary>
public class Attendee : Aggregate
{
    private readonly List<AdditionalDetail> _additionalDetails = [];
    private readonly List<TicketSelection> _tickets = [];

    private Attendee()
    {
    }

    private Attendee(
        Guid id,
        Guid ticketedEventId,
        Guid participantId,
        string email,
        string firstName,
        string lastName,
        List<AdditionalDetail> additionalDetails,
        List<TicketSelection> tickets)
        : base(id)
    {
        TicketedEventId = ticketedEventId;
        ParticipantId = participantId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        RegistrationStatus = RegistrationStatus.Registered;

        _additionalDetails = additionalDetails;
        _tickets = tickets;

        AddDomainEvent(new AttendeeRegisteredDomainEvent(ticketedEventId, participantId, id, email, firstName, lastName));
    }

    public Guid TicketedEventId { get; private set; }
    public Guid ParticipantId { get; private set; }
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public RegistrationStatus RegistrationStatus { get; private set; }

    public IReadOnlyCollection<AdditionalDetail> AdditionalDetails => _additionalDetails.AsReadOnly();
    public IReadOnlyCollection<TicketSelection> Tickets => _tickets.AsReadOnly();

    public static Attendee Create(
        Guid ticketedEventId,
        Guid participantId,
        string email,
        string firstName,
        string lastName,
        IEnumerable<AdditionalDetail> additionalDetails,
        IEnumerable<TicketSelection> tickets)
    {
        return new Attendee(
            Guid.NewGuid(),
            ticketedEventId,
            participantId,
            email,
            firstName,
            lastName,
            additionalDetails.ToList(),
            tickets.ToList());
    }
    
    public void CancelRegistration(CancellationPolicy policy, DateTimeOffset eventStartTime)
    {
        var now = DateTimeOffset.UtcNow;
        if (now >= eventStartTime)
        {
            throw new DomainRuleException(DomainRuleError.Registration.CannotCancelAfterEventStart);
        }
        
        if (RegistrationStatus == RegistrationStatus.Canceled)
        {
            throw new DomainRuleException(DomainRuleError.Registration.AlreadyCanceled);
        }
        
        if (RegistrationStatus != RegistrationStatus.Registered && RegistrationStatus != RegistrationStatus.Reconfirmed)
        {
            throw new DomainRuleException(DomainRuleError.Registration.CannotCancelInStatus(RegistrationStatus));
        }

        RegistrationStatus = RegistrationStatus.Canceled;

        DomainEvent domainEvent = eventStartTime - now < policy.CutoffBeforeEvent
            ? new AttendeeCanceledLateDomainEvent(TicketedEventId, ParticipantId, Id, Email, _tickets)
            : new AttendeeCanceledDomainEvent(TicketedEventId,ParticipantId, Id, Email, _tickets);
        
        AddDomainEvent(domainEvent);
    }

    public void ReconfirmRegistration()
    {
        if (RegistrationStatus != RegistrationStatus.Registered && RegistrationStatus != RegistrationStatus.Reconfirmed)
        {
            throw new DomainRuleException(DomainRuleError.Attendee.CannotReconfirmInStatus(RegistrationStatus));
        }

        if (RegistrationStatus == RegistrationStatus.Reconfirmed) return;
        
        RegistrationStatus = RegistrationStatus.Reconfirmed;

        AddDomainEvent(new AttendeeReconfirmedDomainEvent(TicketedEventId, ParticipantId, Id));
    }

    public void MarkAsCheckedIn()
    {
        RegistrationStatus = RegistrationStatus.CheckedIn;
    }
    
    public void MarkAsNoShow()
    {
        RegistrationStatus = RegistrationStatus.NoShow;
    }
}