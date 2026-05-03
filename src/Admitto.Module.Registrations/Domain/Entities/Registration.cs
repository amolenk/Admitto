using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.Entities;

public class Registration : Aggregate<RegistrationId>
{
    private readonly List<TicketTypeSnapshot> _tickets = [];

    private Registration() { }

    private Registration(
        RegistrationId id,
        TeamId teamId,
        TicketedEventId eventId,
        EmailAddress email,
        FirstName firstName,
        LastName lastName,
        IReadOnlyList<TicketTypeSnapshot> tickets,
        AdditionalDetails additionalDetails)
        : base(id)
    {
        TeamId = teamId;
        EventId = eventId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Status = RegistrationStatus.Registered;
        HasReconfirmed = false;
        ReconfirmedAt = null;
        _tickets = tickets.ToList();
        AdditionalDetails = additionalDetails;

        AddDomainEvent(new AttendeeRegisteredDomainEvent(teamId, eventId, id, email, firstName, lastName, tickets));
    }

    public TeamId TeamId { get; private set; }
    public TicketedEventId EventId { get; private set; }
    public EmailAddress Email { get; private set; }
    public FirstName FirstName { get; private set; }
    public LastName LastName { get; private set; }
    public RegistrationStatus Status { get; private set; }
    public bool HasReconfirmed { get; private set; }
    public DateTimeOffset? ReconfirmedAt { get; private set; }
    public CancellationReason? CancellationReason { get; private set; }
    public IReadOnlyList<TicketTypeSnapshot> Tickets => _tickets.AsReadOnly();
    public AdditionalDetails AdditionalDetails { get; private set; } = AdditionalDetails.Empty;

    public static Registration Create(
        TeamId teamId,
        TicketedEventId eventId,
        EmailAddress email,
        FirstName firstName,
        LastName lastName,
        IReadOnlyList<TicketTypeSnapshot> tickets,
        AdditionalDetails? additionalDetails = null)
    {
        return new Registration(
            RegistrationId.New(),
            teamId,
            eventId,
            email,
            firstName,
            lastName,
            tickets,
            additionalDetails ?? AdditionalDetails.Empty);
    }

    public void Cancel(CancellationReason reason)
    {
        if (Status == RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.AlreadyCancelled);

        Status = RegistrationStatus.Cancelled;
        CancellationReason = reason;

        AddDomainEvent(new RegistrationCancelledDomainEvent(TeamId, EventId, Id, Email, reason));
    }

    public void Reset(
        FirstName firstName,
        LastName lastName,
        IReadOnlyList<TicketTypeSnapshot> tickets,
        AdditionalDetails additionalDetails)
    {
        if (Status != RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.CannotResetActive);

        FirstName = firstName;
        LastName = lastName;
        Status = RegistrationStatus.Registered;
        HasReconfirmed = false;
        ReconfirmedAt = null;
        CancellationReason = null;
        _tickets.Clear();
        _tickets.AddRange(tickets);
        AdditionalDetails = additionalDetails;

        AddDomainEvent(new AttendeeRegisteredDomainEvent(
            TeamId,
            EventId,
            Id,
            Email,
            FirstName,
            LastName,
            tickets));
    }

    public void ChangeTickets(IReadOnlyList<TicketTypeSnapshot> newTickets, DateTimeOffset changedAt)
    {
        if (Status == RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.RegistrationIsCancelled);

        var oldTickets = _tickets.ToList();
        _tickets.Clear();
        _tickets.AddRange(newTickets);

        AddDomainEvent(new TicketsChangedDomainEvent(
            TeamId, EventId, Id, Email, FirstName, LastName,
            oldTickets, newTickets, changedAt));
    }

    public void Reconfirm(DateTimeOffset now)
    {
        if (Status == RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.CannotReconfirmCancelled);

        if (HasReconfirmed)
            return;

        HasReconfirmed = true;
        ReconfirmedAt = now;

        AddDomainEvent(new RegistrationReconfirmedDomainEvent(TeamId, EventId, Id, Email, now));
    }

    internal static class Errors
    {
        public static readonly Error RegistrationIsCancelled = new(
            "registration.is_cancelled",
            "Registration is cancelled.",
            Type: ErrorType.Conflict);

        public static readonly Error AlreadyCancelled = new(
            "registration.already_cancelled",
            "Registration is already cancelled.",
            Type: ErrorType.Conflict);

        public static readonly Error CannotReconfirmCancelled = new(
            "registration.cannot_reconfirm_cancelled",
            "A cancelled registration cannot be reconfirmed.",
            Type: ErrorType.Conflict);

        public static readonly Error CannotResetActive = new(
            "registration.cannot_reset_active",
            "Only a cancelled registration can be reset.",
            Type: ErrorType.Conflict);
    }
}
