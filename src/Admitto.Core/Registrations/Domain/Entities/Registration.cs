using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Domain.Entities;

public class Registration : Aggregate<RegistrationId>
{
    private readonly List<TicketTypeSnapshot> _tickets = [];

    private Registration() { }

    private Registration(
        RegistrationId id,
        TeamId teamId,
        TicketedEventId eventId,
        RegistrationCycleId registrationCycleId,
        EmailAddress email,
        FirstName firstName,
        LastName lastName,
        IReadOnlyList<TicketTypeSnapshot> tickets,
        AdditionalDetails additionalDetails,
        DateTimeOffset registeredAt)
        : base(id)
    {
        TeamId = teamId;
        EventId = eventId;
        RegistrationCycleId = registrationCycleId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Status = RegistrationStatus.Registered;
        HasReconfirmed = false;
        ReconfirmedAt = null;
        _tickets = tickets.ToList();
        AdditionalDetails = additionalDetails;

        AddDomainEvent(new AttendeeRegisteredDomainEvent(teamId, eventId, id, email, firstName, lastName, tickets, registeredAt));
    }

    public TeamId TeamId { get; private set; }
    public TicketedEventId EventId { get; private set; }
    public RegistrationCycleId RegistrationCycleId { get; private set; }
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
        AdditionalDetails? additionalDetails = null,
        DateTimeOffset? registeredAt = null)
    {
        return new Registration(
            RegistrationId.New(),
            teamId,
            eventId,
            RegistrationCycleId.New(),
            email,
            firstName,
            lastName,
            tickets,
            additionalDetails ?? AdditionalDetails.Empty,
            registeredAt ?? DateTimeOffset.UtcNow);
    }

    public void Cancel(CancellationReason reason)
    {
        if (Status == RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.AlreadyCancelled);

        Status = RegistrationStatus.Cancelled;
        CancellationReason = reason;

        AddDomainEvent(new RegistrationCancelledDomainEvent(TeamId, EventId, Id, Email, FirstName, LastName, reason));
    }

    public void Reset(
        FirstName firstName,
        LastName lastName,
        IReadOnlyList<TicketTypeSnapshot> tickets,
        AdditionalDetails additionalDetails,
        DateTimeOffset registeredAt)
    {
        if (Status != RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.CannotResetActive);

        CreatedAt = registeredAt;
        RegistrationCycleId = RegistrationCycleId.New();
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
            tickets,
            registeredAt));
    }

    public void ChangeTickets(IReadOnlyList<TicketTypeSnapshot> newTickets, DateTimeOffset changedAt)
    {
        if (Status == RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.RegistrationIsCancelled);

        var oldTickets = _tickets.ToList();
        _tickets.Clear();
        _tickets.AddRange(newTickets);

        if (HasSameTicketSelection(oldTickets, newTickets))
            return;

        AddDomainEvent(new TicketsChangedDomainEvent(
            TeamId, EventId, Id, Email, FirstName, LastName,
            oldTickets, newTickets, changedAt));
    }

    public void ReplaceAttendeeEditableState(
        FirstName firstName,
        LastName lastName,
        AdditionalDetails additionalDetails,
        IReadOnlyList<TicketTypeSnapshot> newTickets,
        DateTimeOffset changedAt)
    {
        if (Status == RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.RegistrationIsCancelled);

        var oldTickets = _tickets.ToList();

        FirstName = firstName;
        LastName = lastName;
        AdditionalDetails = additionalDetails;
        _tickets.Clear();
        _tickets.AddRange(newTickets);

        if (HasSameTicketSelection(oldTickets, newTickets))
            return;

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

    private static bool HasSameTicketSelection(
        IReadOnlyList<TicketTypeSnapshot> currentTickets,
        IReadOnlyList<TicketTypeSnapshot> newTickets)
    {
        if (currentTickets.Count != newTickets.Count)
            return false;

        var currentIds = currentTickets.Select(t => t.Id).ToHashSet();
        return newTickets.All(t => currentIds.Contains(t.Id));
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
