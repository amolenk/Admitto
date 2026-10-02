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
        DateTimeOffset registeredAt,
        IReadOnlyList<TicketTypeSnapshot> waitlistedTickets)
        : base(id)
    {
        CreatedAt = registeredAt;
        TeamId = teamId;
        EventId = eventId;
        RegistrationCycleId = registrationCycleId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        HasReconfirmed = false;
        ReconfirmedAt = null;
        CheckedInAt = null;
        _tickets = tickets.ToList();
        Status = DeriveStatus(_tickets);
        AdditionalDetails = additionalDetails;
        SearchText = BuildSearchText(email, firstName, lastName);

        AddDomainEvent(new AttendeeRegisteredDomainEvent(
            teamId, eventId, id, email, firstName, lastName, tickets, waitlistedTickets, registeredAt));
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
    public DateTimeOffset? CancelledAt { get; private set; }
    public DateTimeOffset? CheckedInAt { get; private set; }
    public string SearchText { get; private set; } = string.Empty;
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
        DateTimeOffset? registeredAt = null,
        IReadOnlyList<TicketTypeSnapshot>? waitlistedTickets = null,
        RegistrationId? id = null)
    {
        return new Registration(
            id ?? RegistrationId.New(),
            teamId,
            eventId,
            RegistrationCycleId.New(),
            email,
            firstName,
            lastName,
            tickets,
            additionalDetails ?? AdditionalDetails.Empty,
            registeredAt ?? DateTimeOffset.UtcNow,
            waitlistedTickets ?? []);
    }

    public void Cancel(CancellationReason reason)
    {
        if (Status == RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.AlreadyCancelled);

        if (CheckedInAt is not null)
            throw new BusinessRuleViolationException(Errors.CannotCancelCheckedIn);

        var wasWaitlisted = Status == RegistrationStatus.Waitlisted;

        Status = RegistrationStatus.Cancelled;
        CancellationReason = reason;
        CancelledAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new RegistrationCancelledDomainEvent(
            TeamId, EventId, Id, Email, FirstName, LastName, reason, wasWaitlisted));
    }

    /// <summary>
    /// Starts a new registration cycle for this email: a cancelled registration, or a waitlisted one claiming tickets
    /// or rejoining the waitlist, takes the new attendee data and tickets and announces itself as newly registered.
    /// </summary>
    public void Reset(
        FirstName firstName,
        LastName lastName,
        IReadOnlyList<TicketTypeSnapshot> tickets,
        AdditionalDetails additionalDetails,
        DateTimeOffset registeredAt,
        IReadOnlyList<TicketTypeSnapshot>? waitlistedTickets = null)
    {
        if (Status == RegistrationStatus.Registered)
            throw new BusinessRuleViolationException(Errors.CannotResetActive);

        var newTickets = tickets.ToList();

        CreatedAt = registeredAt;
        RegistrationCycleId = RegistrationCycleId.New();
        FirstName = firstName;
        LastName = lastName;
        HasReconfirmed = false;
        ReconfirmedAt = null;
        CancellationReason = null;
        CancelledAt = null;
        CheckedInAt = null;
        _tickets.Clear();
        _tickets.AddRange(newTickets);
        Status = DeriveStatus(_tickets);
        AdditionalDetails = additionalDetails;
        SearchText = BuildSearchText(Email, FirstName, LastName);

        AddDomainEvent(new AttendeeRegisteredDomainEvent(
            TeamId,
            EventId,
            Id,
            Email,
            FirstName,
            LastName,
            newTickets,
            waitlistedTickets ?? [],
            registeredAt));
    }

    /// <remarks>
    /// Waitlist entries are owned by the <see cref="Waitlist"/> aggregates, so the caller supplies the
    /// attendee's waitlisted ticket types before and after the change. A change to either the confirmed
    /// or the waitlisted selection raises <see cref="TicketsChangedDomainEvent"/>.
    /// </remarks>
    public void ChangeTickets(
        IReadOnlyList<TicketTypeSnapshot> newTickets,
        IReadOnlyList<TicketTypeSnapshot> oldWaitlistedTickets,
        IReadOnlyList<TicketTypeSnapshot> newWaitlistedTickets,
        DateTimeOffset changedAt)
    {
        if (Status == RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.RegistrationIsCancelled);

        var oldTickets = _tickets.ToList();
        _tickets.Clear();
        _tickets.AddRange(newTickets);
        Status = DeriveStatus(_tickets);

        RaiseTicketsChangedIfSelectionChanged(
            oldTickets, newTickets, oldWaitlistedTickets, newWaitlistedTickets, changedAt);
    }

    /// <remarks>Waitlisted ticket types are supplied as for <see cref="ChangeTickets"/>.</remarks>
    public void ReplaceAttendeeEditableState(
        FirstName firstName,
        LastName lastName,
        AdditionalDetails additionalDetails,
        IReadOnlyList<TicketTypeSnapshot> newTickets,
        IReadOnlyList<TicketTypeSnapshot> oldWaitlistedTickets,
        IReadOnlyList<TicketTypeSnapshot> newWaitlistedTickets,
        DateTimeOffset changedAt)
    {
        if (Status == RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.RegistrationIsCancelled);

        var oldTickets = _tickets.ToList();

        FirstName = firstName;
        LastName = lastName;
        AdditionalDetails = additionalDetails;
        SearchText = BuildSearchText(Email, FirstName, LastName);
        _tickets.Clear();
        _tickets.AddRange(newTickets);
        Status = DeriveStatus(_tickets);

        RaiseTicketsChangedIfSelectionChanged(
            oldTickets, newTickets, oldWaitlistedTickets, newWaitlistedTickets, changedAt);
    }

    public void Reconfirm(DateTimeOffset now)
    {
        if (Status == RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.CannotReconfirmCancelled);

        if (Status == RegistrationStatus.Waitlisted)
            throw new BusinessRuleViolationException(Errors.CannotReconfirmWaitlisted);

        if (HasReconfirmed)
            return;

        HasReconfirmed = true;
        ReconfirmedAt = now;

        AddDomainEvent(new RegistrationReconfirmedDomainEvent(TeamId, EventId, Id, Email, now));
    }

    public void CheckIn(DateTimeOffset serverNow, CheckInSource source = CheckInSource.Dashboard)
    {
        if (Status == RegistrationStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.CannotCheckInCancelled);

        if (Status == RegistrationStatus.Waitlisted)
            throw new BusinessRuleViolationException(Errors.CannotCheckInWaitlisted);

        if (CheckedInAt is not null)
            return;

        CheckedInAt = serverNow;
        AddDomainEvent(new RegistrationCheckedInDomainEvent(TeamId, EventId, Id, serverNow, source));
    }

    private void RaiseTicketsChangedIfSelectionChanged(
        IReadOnlyList<TicketTypeSnapshot> oldTickets,
        IReadOnlyList<TicketTypeSnapshot> newTickets,
        IReadOnlyList<TicketTypeSnapshot> oldWaitlistedTickets,
        IReadOnlyList<TicketTypeSnapshot> newWaitlistedTickets,
        DateTimeOffset changedAt)
    {
        if (HasSameTicketSelection(oldTickets, newTickets)
            && HasSameTicketSelection(oldWaitlistedTickets, newWaitlistedTickets))
            return;

        AddDomainEvent(new TicketsChangedDomainEvent(
            TeamId, EventId, Id, Email, FirstName, LastName,
            oldTickets, newTickets, oldWaitlistedTickets, newWaitlistedTickets, changedAt));
    }

    private static RegistrationStatus DeriveStatus(IReadOnlyList<TicketTypeSnapshot> tickets) =>
        tickets.Count > 0 ? RegistrationStatus.Registered : RegistrationStatus.Waitlisted;

    private static string BuildSearchText(EmailAddress email, FirstName firstName, LastName lastName) =>
        $"{firstName.Value} {lastName.Value} {email.Value}".ToLowerInvariant();

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

        public static readonly Error CannotCancelCheckedIn = new(
            "registration.cannot_cancel_checked_in",
            "A checked-in registration cannot be cancelled.",
            Type: ErrorType.Conflict);

        public static readonly Error CannotCheckInCancelled = new(
            "registration.cannot_check_in_cancelled",
            "A cancelled registration cannot be checked in.",
            Type: ErrorType.Conflict);

        public static readonly Error CannotCheckInWaitlisted = new(
            "registration.cannot_check_in_waitlisted",
            "A waitlisted registration cannot be checked in.",
            Type: ErrorType.Conflict);

        public static readonly Error CannotReconfirmCancelled = new(
            "registration.cannot_reconfirm_cancelled",
            "A cancelled registration cannot be reconfirmed.",
            Type: ErrorType.Conflict);

        public static readonly Error CannotReconfirmWaitlisted = new(
            "registration.cannot_reconfirm_waitlisted",
            "A waitlisted registration cannot be reconfirmed.",
            Type: ErrorType.Conflict);

        public static readonly Error CannotResetActive = new(
            "registration.cannot_reset_active",
            "Only a cancelled or waitlisted registration can be reset.",
            Type: ErrorType.Conflict);
    }
}
