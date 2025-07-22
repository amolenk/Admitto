using System.Diagnostics;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents a registered attendee for an event.
/// </summary>
public class Attendee : AggregateRoot, IHasAdditionalDetails
{
    private readonly List<AdditionalDetail> _additionalDetails = [];
    private readonly List<TicketSelection> _tickets = [];

    private Attendee()
    {
    }

    private Attendee(
        Guid id,
        Guid teamId,
        Guid ticketedEventId,
        string email,
        string firstName,
        string lastName,
        List<AdditionalDetail> additionalDetails,
        List<TicketSelection> tickets,
        bool isInvited)
        : base(id)
    {
        TeamId = teamId;
        TicketedEventId = ticketedEventId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;

        _additionalDetails = additionalDetails;
        _tickets = tickets;

        if (isInvited)
        {
            AddDomainEvent(new AttendeeInvitedDomainEvent(TicketedEventId, Id, _tickets));
            Status = AttendeeStatus.PendingTickets;
        }
        else
        {
            AddDomainEvent(new AttendeeSignedUpDomainEvent(TeamId, TicketedEventId, Id));

            EmailVerification = EmailVerification.Generate();
            Status = AttendeeStatus.PendingVerification;
        }
    }

    public Guid TeamId { get; private set; }
    public Guid TicketedEventId { get; private set; }
    public string Email { get; private set; } = null!;
    public EmailVerification? EmailVerification { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public AttendeeStatus Status { get; private set; }
    public AttendeeParticipation? Participation { get; private set; }
    public DateTimeOffset? ReconfirmedAt { get; private set; }
    public DateTimeOffset? CheckedInAt { get; private set; }
    public DateTimeOffset? CanceledAt { get; private set; }

    public IReadOnlyCollection<AdditionalDetail> AdditionalDetails => _additionalDetails.AsReadOnly();
    public IReadOnlyCollection<TicketSelection> Tickets => _tickets.AsReadOnly();

    public static Attendee Create(
        Guid teamId,
        Guid ticketedEventId,
        string email,
        string firstName,
        string lastName,
        IEnumerable<AdditionalDetail> additionalDetails,
        IEnumerable<TicketSelection> tickets,
        bool isInvited = false)
    {
        return new Attendee(
            Guid.NewGuid(),
            teamId,
            ticketedEventId,
            email,
            firstName,
            lastName,
            additionalDetails.ToList(),
            tickets.ToList(),
            isInvited);
    }
    
    public bool Verify(string code)
    {
        if (Status == AttendeeStatus.PendingVerification
            && EmailVerification is not null
            && !EmailVerification.IsExpired
            && EmailVerification.Code == code)
        {
            // Mark the registration as verified.
            AddDomainEvent(new AttendeeVerifiedDomainEvent(TicketedEventId, Id, _tickets));
            Status = AttendeeStatus.PendingTickets;
            return true;
        }

        Status = AttendeeStatus.VerificationFailed;
        return false;
    }
    
    public void CompleteRegistration()
    {
        EnsureTransitionsFromPendingTickets();
        AddDomainEvent(new RegistrationCompletedDomainEvent(TeamId, TicketedEventId, Id));
        Status = AttendeeStatus.Registered;
    }

    public void FailRegistration()
    {
        EnsureTransitionsFromPendingTickets();
        AddDomainEvent(new RegistrationFailedDomainEvent(TeamId, TicketedEventId, Id));
        Status = AttendeeStatus.RegistrationFailed;
    }

    // public void CancelRegistration(DateTimeOffset maxCancellationTime, DateTimeOffset? canceledAt = null)
    // {
    //     if (Status != AttendeeStatus.Registered)
    //     {
    //         throw new BusinessRuleException(BusinessRuleError.Registration.MustBeCompletedBeforeCancellation);
    //     }
    //
    //     if (Participation is not null)
    //     {
    //         throw new BusinessRuleException(BusinessRuleError.Registration.CannotCancelAfterParticipation);
    //     }
    //     
    //     Status = AttendeeStatus.Canceled;
    //     CanceledAt = canceledAt ?? DateTimeOffset.UtcNow;
    //     
    //     AddDomainEvent(new AttendeeCanceledDomainEvent(
    //         TeamId,
    //         TicketedEventId,
    //         Id,
    //         LateCancellation: CanceledAt > maxCancellationTime,
    //         Version));
    // }

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
    
    private void EnsureTransitionsFromPendingTickets()
    {
        if (Status != AttendeeStatus.PendingTickets)
        {
            throw new BusinessRuleException(BusinessRuleError.Attendee.StatusMismatch(
                AttendeeStatus.PendingTickets, Status));
        }
    }
}