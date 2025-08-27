using Amolenk.Admitto.Domain.Contracts;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents a registration for a ticketed event.
/// </summary>
public class AttendeeRegistration : AggregateRoot, IHasAdditionalDetails
{
    private readonly List<AdditionalDetail> _additionalDetails = [];
    private readonly List<TicketSelection> _tickets = [];

    private AttendeeRegistration()
    {
    }

    private AttendeeRegistration(
        Guid id,
        Guid teamId,
        Guid ticketedEventId,
        string email,
        string firstName,
        string lastName,
        List<AdditionalDetail> additionalDetails,
        List<TicketSelection> tickets)
        : base(id)
    {
        TeamId = teamId;
        TicketedEventId = ticketedEventId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;

        _additionalDetails = additionalDetails;
        _tickets = tickets;

        AddDomainEvent(new RegistrationCompletedDomainEvent(TeamId, TicketedEventId, Id));
        Status = RegistrationStatus.Registered;
    }

    public Guid TeamId { get; private set; }
    public Guid TicketedEventId { get; private set; }
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public RegistrationStatus Status { get; private set; }

    public IReadOnlyCollection<AdditionalDetail> AdditionalDetails => _additionalDetails.AsReadOnly();
    public IReadOnlyCollection<TicketSelection> Tickets => _tickets.AsReadOnly();

    public static AttendeeRegistration Create(
        Guid teamId,
        Guid ticketedEventId,
        Guid registrationId,
        string email,
        string firstName,
        string lastName,
        IEnumerable<AdditionalDetail> additionalDetails,
        IEnumerable<TicketSelection> tickets)
    {
        return new AttendeeRegistration(
            registrationId,
            teamId,
            ticketedEventId,
            email,
            firstName,
            lastName,
            additionalDetails.ToList(),
            tickets.ToList());
    }
    
    public void Cancel(CancellationPolicy policy, DateTimeOffset eventStartTime)
    {
        var now = DateTimeOffset.UtcNow;
        if (now >= eventStartTime)
        {
            throw new DomainRuleException(DomainRuleError.Registration.CannotCancelAfterEventStart);
        }
        
        if (Status == RegistrationStatus.Canceled)
        {
            throw new DomainRuleException(DomainRuleError.Registration.AlreadyCanceled);
        }
        
        if (Status != RegistrationStatus.Registered && Status != RegistrationStatus.Reconfirmed)
        {
            throw new DomainRuleException(DomainRuleError.Registration.CannotCancelInStatus(Status));
        }

        Status = RegistrationStatus.Canceled;

        DomainEvent domainEvent = eventStartTime - now < policy.LateCancellationTime
            ? new AttendeeCanceledLateDomainEvent(TeamId, TicketedEventId, Id, Version, Email, _tickets)
            : new AttendeeCanceledDomainEvent(TeamId, TicketedEventId, Id, Version, Email, _tickets);
        
        AddDomainEvent(domainEvent);
    }

    // public void ReconfirmRegistration(DateTimeOffset? reconfirmedAt = null)
    // {
    //     if (Status != AttendeeStatus.Registered)
    //     {
    //         throw new BusinessRuleException(BusinessRuleError.Registration.MustBeCompletedBeforeReconfirming);
    //     }
    //
    //     if (Participation is not null)
    //     {
    //         throw new BusinessRuleException(BusinessRuleError.Registration.CannotReconfirmAfterParticipation);
    //     }
    //
    //     reconfirmedAt ??= DateTimeOffset.UtcNow;
    //     if (ReconfirmedAt is null || ReconfirmedAt < reconfirmedAt)
    //     {
    //         ReconfirmedAt = reconfirmedAt;
    //     }
    // }

    // TODO Allow ignore event start time when checking in.
    // public void CheckIn(DateTimeOffset? checkedInAt = null)
    // {
    //     if (Status != AttendeeStatus.Registered)
    //     {
    //         throw new BusinessRuleException(BusinessRuleError.Registration.MustBeCompletedBeforeCheckIn);
    //     }
    //
    //     Participation = AttendeeParticipation.CheckedIn;
    //     if (CheckedInAt is null)
    //     {
    //         checkedInAt = checkedInAt ?? DateTimeOffset.UtcNow;
    //     }
    //     
    //     AddDomainEvent(new AttendeeCheckedInDomainEvent(
    //         TeamId,
    //         TicketedEventId,
    //         Id,
    //         Version));
    // }

    // public void MarkAsNoShow()
    // {
    //     if (Status != AttendeeStatus.Registered)
    //     {
    //         throw new BusinessRuleException(BusinessRuleError.Registration.MustBeCompletedBeforeCheckIn);
    //     }
    //
    //     Participation = AttendeeParticipation.NoShow;
    //     CheckedInAt = null;
    //
    //     AddDomainEvent(new AttendeeNoShowDomainEvent(
    //         TeamId,
    //         TicketedEventId,
    //         Id,
    //         Version));
    // }
}