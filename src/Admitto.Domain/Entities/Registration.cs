using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents a registration for a ticketed event.
/// </summary>
public class Registration : AggregateRoot, IHasAdditionalDetails
{
    private readonly List<AdditionalDetail> _additionalDetails = [];
    private readonly List<TicketSelection> _tickets = [];

    private Registration()
    {
    }

    private Registration(
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

    public static Registration Create(
        Guid teamId,
        Guid ticketedEventId,
        Guid registrationId,
        string email,
        string firstName,
        string lastName,
        IEnumerable<AdditionalDetail> additionalDetails,
        IEnumerable<TicketSelection> tickets)
    {
        return new Registration(
            registrationId,
            teamId,
            ticketedEventId,
            email,
            firstName,
            lastName,
            additionalDetails.ToList(),
            tickets.ToList());
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
}