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

    [TestMethod]
    public void Registration_CancelWithReconfirmAutoCancel_TransitionsAndStoresReason()
    {
        var sut = NewRegistration();

        sut.Cancel(CancellationReason.ReconfirmAutoCancel);

        sut.Status.ShouldBe(RegistrationStatus.Cancelled);
        sut.CancellationReason.ShouldBe(CancellationReason.ReconfirmAutoCancel);
    }

    [TestMethod]
    public void Registration_CancelTwice_Throws()
    {
        var sut = NewRegistration();
        sut.Cancel(CancellationReason.AttendeeRequest);

        var result = ErrorResult.Capture(() => sut.Cancel(CancellationReason.AttendeeRequest));

        result.Error.ShouldMatch(Registration.Errors.AlreadyCancelled);
    }

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

    [TestMethod]
    public void Registration_ReconfirmAfterCancel_Throws()
    {
        var sut = NewRegistration();
        sut.Cancel(CancellationReason.AttendeeRequest);

        var result = ErrorResult.Capture(() => sut.Reconfirm(DateTimeOffset.UtcNow));

        result.Error.ShouldMatch(Registration.Errors.CannotReconfirmCancelled);
    }

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

    [TestMethod]
    public void Registration_ChangeTickets_SameSelection_StillRaisesEvent()
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
        sut.GetDomainEvents().OfType<TicketsChangedDomainEvent>().ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Registration_ChangeTickets_Cancelled_Throws()
    {
        var sut = NewRegistration();
        sut.Cancel(CancellationReason.AttendeeRequest);

        var result = ErrorResult.Capture(() =>
            sut.ChangeTickets([new(TicketTypeId.New(), TicketTypeName.From("Workshop"), [])], DateTimeOffset.UtcNow));

        result.Error.ShouldMatch(Registration.Errors.RegistrationIsCancelled);
    }

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

        sut.Reset(
            FirstName.From("Reset"),
            LastName.From("User"),
            [new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("Workshop"), [TimeSlot.From("morning")])],
            AdditionalDetails.From(new Dictionary<string, string> { ["tshirt"] = "M" }),
            DateTimeOffset.UtcNow);

        sut.Id.ShouldBe(id);
        sut.TeamId.ShouldBe(teamId);
        sut.EventId.ShouldBe(eventId);
        sut.Email.ShouldBe(email);
        sut.Status.ShouldBe(RegistrationStatus.Registered);
    }

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
