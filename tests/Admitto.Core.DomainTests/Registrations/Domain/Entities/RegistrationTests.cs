using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Core.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class RegistrationTests
{
    private static readonly TeamId DefaultTeamId = TeamId.New();
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly EmailAddress DefaultEmail = EmailAddress.From("test@example.com");
    private static readonly FirstName DefaultFirstName = FirstName.From("Test");
    private static readonly LastName DefaultLastName = LastName.From("User");

    // Given two distinct ticket type snapshots with different time slots
    // When a registration is created with those tickets
    // Then each ticket's id and time slots are preserved on the registration
    [TestMethod]
    public void Registration_Create_ValidInput_CreatesWithCorrectSnapshots()
    {
        // Arrange
        var id1 = TicketTypeId.New();
        var id2 = TicketTypeId.New();
        var timeSlots1 = new[] { TimeSlot.From("morning"), TimeSlot.From("afternoon") };
        var timeSlots2 = new[] { TimeSlot.From("evening") };

        var tickets = new List<TicketTypeSnapshot>
        {
            new(id1, TicketTypeName.From("General Admission"), timeSlots1),
            new(id2, TicketTypeName.From("VIP Pass"), timeSlots2)
        };

        // Act
        var sut = Registration.Create(DefaultTeamId, DefaultEventId, DefaultEmail, DefaultFirstName, DefaultLastName, tickets);

        // Assert
        sut.Tickets.Count.ShouldBe(2);
        sut.Tickets.ShouldContain(t => t.Id == id1 && t.TimeSlots.SequenceEqual(timeSlots1));
        sut.Tickets.ShouldContain(t => t.Id == id2 && t.TimeSlots.SequenceEqual(timeSlots2));
    }

    // When a new registration is created
    // Then it has the given identity and starts Registered with no reconfirmation or cancellation
    [TestMethod]
    public void Registration_Create_PopulatesIdentityAndDefaults()
    {
        var sut = NewRegistration();

        sut.Email.ShouldBe(DefaultEmail);
        sut.FirstName.ShouldBe(DefaultFirstName);
        sut.LastName.ShouldBe(DefaultLastName);
        sut.Status.ShouldBe(RegistrationStatus.Registered);
        sut.HasReconfirmed.ShouldBeFalse();
        sut.ReconfirmedAt.ShouldBeNull();
        sut.CancellationReason.ShouldBeNull();
    }

    // Given an active registration
    // When it is cancelled at the attendee's request
    // Then its status becomes Cancelled with that reason and a RegistrationCancelled event is raised
    [TestMethod]
    public void Registration_Cancel_TransitionsAndRaisesEvent()
    {
        var sut = NewRegistration();
        ClearEvents(sut);

        sut.Cancel(CancellationReason.AttendeeRequest);

        sut.Status.ShouldBe(RegistrationStatus.Cancelled);
        sut.CancellationReason.ShouldBe(CancellationReason.AttendeeRequest);
        sut.GetDomainEvents().OfType<RegistrationCancelledDomainEvent>().ShouldHaveSingleItem();
    }

    // Given an active registration
    // When it is cancelled due to reconfirm auto-cancel
    // Then its status becomes Cancelled with that specific reason
    [TestMethod]
    public void Registration_CancelWithReconfirmAutoCancel_TransitionsAndStoresReason()
    {
        var sut = NewRegistration();

        sut.Cancel(CancellationReason.ReconfirmAutoCancel);

        sut.Status.ShouldBe(RegistrationStatus.Cancelled);
        sut.CancellationReason.ShouldBe(CancellationReason.ReconfirmAutoCancel);
    }

    // Given a registration that has already been cancelled
    // When cancellation is attempted again
    // Then it returns an AlreadyCancelled error
    [TestMethod]
    public void Registration_CancelTwice_Throws()
    {
        var sut = NewRegistration();
        sut.Cancel(CancellationReason.AttendeeRequest);

        var result = ErrorResult.Capture(() => sut.Cancel(CancellationReason.AttendeeRequest));

        result.Error.ShouldMatch(Registration.Errors.AlreadyCancelled);
    }

    // Given an active, not-yet-reconfirmed registration
    // When it is reconfirmed at a given time
    // Then it is marked reconfirmed at that time and a RegistrationReconfirmed event is raised
    [TestMethod]
    public void Registration_Reconfirm_SetsFlagAndRaisesEvent()
    {
        var sut = NewRegistration();
        ClearEvents(sut);
        var now = DateTimeOffset.UtcNow;

        sut.Reconfirm(now);

        sut.HasReconfirmed.ShouldBeTrue();
        sut.ReconfirmedAt.ShouldBe(now);
        sut.GetDomainEvents().OfType<RegistrationReconfirmedDomainEvent>().ShouldHaveSingleItem();
    }

    // Given a registration that has already been reconfirmed once
    // When it is reconfirmed again at a later time
    // Then the original reconfirmation timestamp is kept and no new event is raised
    [TestMethod]
    public void Registration_ReconfirmTwice_IsIdempotent()
    {
        var sut = NewRegistration();
        var first = DateTimeOffset.UtcNow;
        sut.Reconfirm(first);
        ClearEvents(sut);

        sut.Reconfirm(first.AddHours(1));

        sut.ReconfirmedAt.ShouldBe(first);
        sut.GetDomainEvents().OfType<RegistrationReconfirmedDomainEvent>().ShouldBeEmpty();
    }

    // Given a registration that has been cancelled
    // When reconfirmation is attempted
    // Then it returns a CannotReconfirmCancelled error
    [TestMethod]
    public void Registration_ReconfirmAfterCancel_Throws()
    {
        var sut = NewRegistration();
        sut.Cancel(CancellationReason.AttendeeRequest);

        var result = ErrorResult.Capture(() => sut.Reconfirm(DateTimeOffset.UtcNow));

        result.Error.ShouldMatch(Registration.Errors.CannotReconfirmCancelled);
    }

    // Given an active registration with its original ticket selection
    // When the tickets are changed to a different set
    // Then the registration reflects the new tickets and a TicketsChanged event is raised
    [TestMethod]
    public void Registration_ChangeTickets_HappyPath_UpdatesSnapshotAndRaisesEvent()
    {
        var sut = NewRegistration();
        ClearEvents(sut);
        var workshopId = TicketTypeId.New();
        var dinnerId = TicketTypeId.New();
        var newTickets = new List<TicketTypeSnapshot>
        {
            new(workshopId, TicketTypeName.From("Workshop"), []),
            new(dinnerId, TicketTypeName.From("Dinner"), [])
        };
        var changedAt = DateTimeOffset.UtcNow;

        sut.ChangeTickets(newTickets, changedAt);

        sut.Tickets.Count.ShouldBe(2);
        sut.Tickets.ShouldContain(t => t.Id == workshopId);
        sut.Tickets.ShouldContain(t => t.Id == dinnerId);
        sut.GetDomainEvents().OfType<TicketsChangedDomainEvent>().ShouldHaveSingleItem();
    }

    // Given a registration with a given ticket selection
    // When the tickets are "changed" to the exact same selection
    // Then the tickets remain unchanged and no TicketsChanged event is raised
    [TestMethod]
    public void Registration_ChangeTickets_SameSelection_DoesNotRaiseEvent()
    {
        var generalId = TicketTypeId.New();
        var sut = Registration.Create(DefaultTeamId, DefaultEventId, DefaultEmail, DefaultFirstName, DefaultLastName,
            [new TicketTypeSnapshot(generalId, TicketTypeName.From("General Admission"), [])]);
        ClearEvents(sut);
        var sameTickets = new List<TicketTypeSnapshot>
        {
            new(generalId, TicketTypeName.From("General Admission"), [])
        };

        sut.ChangeTickets(sameTickets, DateTimeOffset.UtcNow);

        sut.Tickets.Count.ShouldBe(1);
        sut.GetDomainEvents().OfType<TicketsChangedDomainEvent>().ShouldBeEmpty();
    }

    // Given an active registration
    // When the attendee's name, additional details, and tickets are replaced with new values
    // Then the registration reflects all new values and a TicketsChanged event is raised
    [TestMethod]
    public void Registration_ReplaceAttendeeEditableState_ValidInput_ReplacesDetailsAndTickets()
    {
        var sut = NewRegistration();
        ClearEvents(sut);
        var workshopId = TicketTypeId.New();

        sut.ReplaceAttendeeEditableState(
            FirstName.From("Alice"),
            LastName.From("Anderson"),
            AdditionalDetails.From(new Dictionary<string, string> { ["dietary"] = "vegan" }),
            [new TicketTypeSnapshot(workshopId, TicketTypeName.From("Workshop"), [TimeSlot.From("morning")])],
            DateTimeOffset.UtcNow);

        sut.FirstName.ShouldBe(FirstName.From("Alice"));
        sut.LastName.ShouldBe(LastName.From("Anderson"));
        sut.AdditionalDetails["dietary"].ShouldBe("vegan");
        sut.Tickets.ShouldHaveSingleItem().Id.ShouldBe(workshopId);
        sut.GetDomainEvents().OfType<TicketsChangedDomainEvent>().ShouldHaveSingleItem();
    }

    // Given a registration with an existing ticket selection
    // When only the name and additional details are replaced while keeping the same tickets
    // Then the details update but no TicketsChanged event is raised
    [TestMethod]
    public void Registration_ReplaceAttendeeEditableState_DetailsOnly_DoesNotRaiseTicketChangeEvent()
    {
        var generalId = TicketTypeId.New();
        var sut = Registration.Create(DefaultTeamId, DefaultEventId, DefaultEmail, DefaultFirstName, DefaultLastName,
            [new TicketTypeSnapshot(generalId, TicketTypeName.From("General Admission"), [])]);
        ClearEvents(sut);

        sut.ReplaceAttendeeEditableState(
            FirstName.From("Alice"),
            LastName.From("Anderson"),
            AdditionalDetails.From(new Dictionary<string, string> { ["dietary"] = "vegan" }),
            [new TicketTypeSnapshot(generalId, TicketTypeName.From("General Admission"), [])],
            DateTimeOffset.UtcNow);

        sut.LastName.ShouldBe(LastName.From("Anderson"));
        sut.AdditionalDetails["dietary"].ShouldBe("vegan");
        sut.GetDomainEvents().OfType<TicketsChangedDomainEvent>().ShouldBeEmpty();
    }

    // Given a registration that has been cancelled
    // When the attendee-editable state is replaced
    // Then it returns a RegistrationIsCancelled error
    [TestMethod]
    public void Registration_ReplaceAttendeeEditableState_Cancelled_Throws()
    {
        var sut = NewRegistration();
        sut.Cancel(CancellationReason.AttendeeRequest);

        var result = ErrorResult.Capture(() => sut.ReplaceAttendeeEditableState(
            FirstName.From("Alice"),
            LastName.From("Anderson"),
            AdditionalDetails.Empty,
            [new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("Workshop"), [])],
            DateTimeOffset.UtcNow));

        result.Error.ShouldMatch(Registration.Errors.RegistrationIsCancelled);
    }

    // Given a registration that has been cancelled
    // When the tickets are changed
    // Then it returns a RegistrationIsCancelled error
    [TestMethod]
    public void Registration_ChangeTickets_Cancelled_Throws()
    {
        var sut = NewRegistration();
        sut.Cancel(CancellationReason.AttendeeRequest);

        var result = ErrorResult.Capture(() =>
            sut.ChangeTickets([new(TicketTypeId.New(), TicketTypeName.From("Workshop"), [])], DateTimeOffset.UtcNow));

        result.Error.ShouldMatch(Registration.Errors.RegistrationIsCancelled);
    }

    // Given a cancelled registration
    // When it is reset with new attendee data and tickets
    // Then its identity fields are preserved and its status returns to Registered
    [TestMethod]
    public void Reset_CancelledRegistration_PreservesIdentityAndRestoresRegisteredStatus()
    {
        var sut = NewRegistration();
        var id = sut.Id;
        var teamId = sut.TeamId;
        var eventId = sut.EventId;
        var email = sut.Email;
        sut.Cancel(CancellationReason.AttendeeRequest);
        ClearEvents(sut);
        var resetAt = DateTimeOffset.UtcNow;

        sut.Reset(
            FirstName.From("Reset"),
            LastName.From("User"),
            [new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("Workshop"), [TimeSlot.From("morning")])],
            AdditionalDetails.From(new Dictionary<string, string> { ["tshirt"] = "M" }),
            resetAt);

        sut.Id.ShouldBe(id);
        sut.TeamId.ShouldBe(teamId);
        sut.EventId.ShouldBe(eventId);
        sut.Email.ShouldBe(email);
        sut.Status.ShouldBe(RegistrationStatus.Registered);
        sut.CreatedAt.ShouldBe(resetAt);
    }

    // Given a cancelled registration with an earlier creation time
    // When it is reset at a supplied time
    // Then its creation time and attendee-registered event use the reset time while its id is preserved
    [TestMethod]
    public void Reset_CancelledRegistration_RefreshesCreatedAtAndEventTime()
    {
        var sut = NewRegistration();
        var originalCreatedAt = DateTimeOffset.UtcNow.AddDays(-1);
        sut.CreatedAt = originalCreatedAt;
        sut.Cancel(CancellationReason.AttendeeRequest);
        ClearEvents(sut);
        var id = sut.Id;
        var resetAt = DateTimeOffset.UtcNow;

        sut.Reset(
            FirstName.From("Reset"),
            LastName.From("User"),
            [new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("Workshop"), [])],
            AdditionalDetails.Empty,
            resetAt);

        sut.Id.ShouldBe(id);
        sut.CreatedAt.ShouldBe(resetAt);
        sut.GetDomainEvents()
            .OfType<AttendeeRegisteredDomainEvent>()
            .ShouldHaveSingleItem()
            .RegisteredAt.ShouldBe(resetAt);
    }

    // Given a registration that is still active (not cancelled)
    // When a reset is attempted
    // Then it returns a CannotResetActive error
    [TestMethod]
    public void Reset_ActiveRegistration_ThrowsCannotResetActive()
    {
        var sut = NewRegistration();

        var result = ErrorResult.Capture(() => sut.Reset(
            FirstName.From("Reset"),
            LastName.From("User"),
            [new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("Workshop"), [])],
            AdditionalDetails.Empty,
            DateTimeOffset.UtcNow));

        result.Error.ShouldMatch(Registration.Errors.CannotResetActive);
    }

    // Given a registration that was reconfirmed and then cancelled
    // When it is reset
    // Then its cancellation reason and reconfirmation state are cleared
    [TestMethod]
    public void Reset_CancelledAndReconfirmedRegistration_ClearsCancellationAndReconfirmationState()
    {
        var sut = NewRegistration();
        sut.Reconfirm(DateTimeOffset.UtcNow);
        sut.Cancel(CancellationReason.AttendeeRequest);

        sut.Reset(
            FirstName.From("Reset"),
            LastName.From("User"),
            [new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("Workshop"), [])],
            AdditionalDetails.Empty,
            DateTimeOffset.UtcNow);

        sut.CancellationReason.ShouldBeNull();
        sut.HasReconfirmed.ShouldBeFalse();
        sut.ReconfirmedAt.ShouldBeNull();
    }

    // Given a cancelled registration with old tickets and additional details
    // When it is reset with a new name, new tickets, and new additional details
    // Then the registration reflects only the new name, tickets, and details
    [TestMethod]
    public void Reset_CancelledRegistration_ReplacesAttendeeTicketsAndAdditionalDetails()
    {
        var oldId = TicketTypeId.New();
        var sut = Registration.Create(
            DefaultTeamId,
            DefaultEventId,
            DefaultEmail,
            FirstName.From("Old"),
            LastName.From("Name"),
            [new TicketTypeSnapshot(oldId, TicketTypeName.From("Old Ticket"), [TimeSlot.From("old-slot")])],
            AdditionalDetails.From(new Dictionary<string, string> { ["meal"] = "vegan" }));
        sut.Cancel(CancellationReason.AttendeeRequest);

        var workshopId = TicketTypeId.New();
        var dinnerId = TicketTypeId.New();
        sut.Reset(
            FirstName.From("New"),
            LastName.From("Person"),
            [
                new TicketTypeSnapshot(workshopId, TicketTypeName.From("Workshop"), [TimeSlot.From("morning")]),
                new TicketTypeSnapshot(dinnerId, TicketTypeName.From("Dinner"), [])
            ],
            AdditionalDetails.From(new Dictionary<string, string> { ["tshirt"] = "M" }),
            DateTimeOffset.UtcNow);

        sut.FirstName.ShouldBe(FirstName.From("New"));
        sut.LastName.ShouldBe(LastName.From("Person"));
        sut.Tickets.Count.ShouldBe(2);
        sut.Tickets.ShouldContain(t => t.Id == workshopId && t.Name.Value == "Workshop");
        sut.Tickets.ShouldContain(t => t.Id == dinnerId && t.Name.Value == "Dinner");
        sut.AdditionalDetails.Count.ShouldBe(1);
        sut.AdditionalDetails["tshirt"].ShouldBe("M");
    }

    // Given a cancelled registration
    // When it is reset with new attendee data and tickets at a given time
    // Then an AttendeeRegistered domain event is raised carrying the current registration data
    [TestMethod]
    public void Reset_CancelledRegistration_RaisesAttendeeRegisteredDomainEventWithCurrentData()
    {
        var sut = NewRegistration();
        sut.Cancel(CancellationReason.AttendeeRequest);
        ClearEvents(sut);
        var workshopId = TicketTypeId.New();
        var tickets = new List<TicketTypeSnapshot>
        {
            new(workshopId, TicketTypeName.From("Workshop"), [TimeSlot.From("morning")])
        };

        var resetAt = DateTimeOffset.UtcNow;
        sut.Reset(
            FirstName.From("Reset"),
            LastName.From("User"),
            tickets,
            AdditionalDetails.Empty,
            resetAt);

        var domainEvent = sut.GetDomainEvents()
            .OfType<AttendeeRegisteredDomainEvent>()
            .ShouldHaveSingleItem();
        domainEvent.TeamId.ShouldBe(sut.TeamId);
        domainEvent.TicketedEventId.ShouldBe(sut.EventId);
        domainEvent.RegistrationId.ShouldBe(sut.Id);
        domainEvent.RecipientEmail.ShouldBe(sut.Email);
        domainEvent.FirstName.ShouldBe(FirstName.From("Reset"));
        domainEvent.LastName.ShouldBe(LastName.From("User"));
        domainEvent.Tickets.ShouldBe(tickets);
        domainEvent.RegisteredAt.ShouldBe(resetAt);
    }

    private static Registration NewRegistration() =>
        Registration.Create(
            DefaultTeamId,
            DefaultEventId,
            DefaultEmail,
            DefaultFirstName,
            DefaultLastName,
            [new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("General Admission"), [])]);

    private static void ClearEvents(Registration r) => r.ClearDomainEvents();
}
