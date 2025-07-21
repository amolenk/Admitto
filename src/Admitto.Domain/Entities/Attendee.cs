using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Exceptions;
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
        EmailVerification = EmailVerification.Generate();
        FirstName = firstName;
        LastName = lastName;

        _additionalDetails = additionalDetails;
        _tickets = tickets;

        if (isInvited)
        {
            // TODO Don't need to set verification properties if the attendee is invited.
         
            AddDomainEvent(new AttendeeInvitedDomainEvent(TicketedEventId, Id, _tickets));
            Status = AttendeeStatus.Verified;
        }
        else
        {
            AddDomainEvent(new AttendeeSignedUpDomainEvent(TeamId, TicketedEventId, Id));
            Status = AttendeeStatus.Unverified;
        }
    }

    public Guid TeamId { get; private set; }
    public Guid TicketedEventId { get; private set; }
    public string Email { get; private set; } = null!;
    public EmailVerification EmailVerification { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public AttendeeStatus Status { get; private set; }

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
        if (Status == AttendeeStatus.Unverified
            && !EmailVerification.IsExpired
            && EmailVerification.Code == code)
        {
            // Mark the registration as verified.
            AddDomainEvent(new AttendeeVerifiedDomainEvent(TicketedEventId, Id, _tickets));
            Status = AttendeeStatus.Verified;
            return true;
        }

        Status = AttendeeStatus.VerificationFailed;
        return false;
    }

    public void CompleteRegistration()
    {
        if (Status != AttendeeStatus.Verified)
        {
            throw new BusinessRuleException("Cannot complete a registration that is not verified.");
        }
        
        AddDomainEvent(new RegistrationCompletedDomainEvent(TeamId, TicketedEventId, Id));
        Status = AttendeeStatus.Registered;
    }

    public void RejectRegistration()
    {
        if (Status != AttendeeStatus.Verified)
        {
            throw new BusinessRuleException("Cannot reject a registration that is not verified.");
        }
        
        AddDomainEvent(new RegistrationRejectedDomainEvent(TeamId, TicketedEventId, Id));
        Status = AttendeeStatus.Rejected;
    }
}