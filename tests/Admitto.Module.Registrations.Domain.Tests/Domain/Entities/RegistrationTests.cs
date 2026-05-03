using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Module.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class RegistrationTests
{
    private static readonly TeamId DefaultTeamId = TeamId.New();
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly EmailAddress DefaultEmail = EmailAddress.From("test@example.com");
    private static readonly FirstName DefaultFirstName = FirstName.From("Test");
    private static readonly LastName DefaultLastName = LastName.From("User");

    [TestMethod]
    public void SC001_Registration_Create_ValidInput_CreatesWithCorrectSnapshots()
    {
        // Arrange
        var slug1 = "general-admission";
        var slug2 = "vip-pass";
        var timeSlots1 = new[] { "morning", "afternoon" };
        var timeSlots2 = new[] { "evening" };

        var tickets = new List<TicketTypeSnapshot>
        {
            new(slug1, slug1, timeSlots1),
            new(slug2, slug2, timeSlots2)
        };

        // Act
        var sut = Registration.Create(DefaultTeamId, DefaultEventId, DefaultEmail, DefaultFirstName, DefaultLastName, tickets);

        // Assert
        sut.Tickets.Count.ShouldBe(2);
        sut.Tickets.ShouldContain(t => t.Slug == slug1 && t.TimeSlots.SequenceEqual(timeSlots1));
        sut.Tickets.ShouldContain(t => t.Slug == slug2 && t.TimeSlots.SequenceEqual(timeSlots2));
    }

    [TestMethod]
    public void SC003_Registration_Create_PopulatesIdentityAndDefaults()
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
    public void SC004_Registration_Cancel_TransitionsAndRaisesEvent()
    {
        var sut = NewRegistration();
        ClearEvents(sut);

        sut.Cancel(CancellationReason.AttendeeRequest);

        sut.Status.ShouldBe(RegistrationStatus.Cancelled);
        sut.CancellationReason.ShouldBe(CancellationReason.AttendeeRequest);
        sut.GetDomainEvents().OfType<RegistrationCancelledDomainEvent>().ShouldHaveSingleItem();
    }

    [TestMethod]
    public void SC005_Registration_CancelTwice_Throws()
    {
        var sut = NewRegistration();
        sut.Cancel(CancellationReason.AttendeeRequest);

        var result = ErrorResult.Capture(() => sut.Cancel(CancellationReason.AttendeeRequest));

        result.Error.ShouldMatch(Registration.Errors.AlreadyCancelled);
    }

    [TestMethod]
    public void SC006_Registration_Reconfirm_SetsFlagAndRaisesEvent()
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
    public void SC007_Registration_ReconfirmTwice_IsIdempotent()
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
    public void SC008_Registration_ReconfirmAfterCancel_Throws()
    {
        var sut = NewRegistration();
        sut.Cancel(CancellationReason.AttendeeRequest);

        var result = ErrorResult.Capture(() => sut.Reconfirm(DateTimeOffset.UtcNow));

        result.Error.ShouldMatch(Registration.Errors.CannotReconfirmCancelled);
    }

    [TestMethod]
    public void SC009_Registration_ChangeTickets_HappyPath_UpdatesSnapshotAndRaisesEvent()
    {
        var sut = NewRegistration();
        ClearEvents(sut);
        var newTickets = new List<TicketTypeSnapshot>
        {
            new("workshop", "Workshop", []),
            new("dinner", "Dinner", [])
        };
        var changedAt = DateTimeOffset.UtcNow;

        sut.ChangeTickets(newTickets, changedAt);

        sut.Tickets.Count.ShouldBe(2);
        sut.Tickets.ShouldContain(t => t.Slug == "workshop");
        sut.Tickets.ShouldContain(t => t.Slug == "dinner");
        sut.GetDomainEvents().OfType<TicketsChangedDomainEvent>().ShouldHaveSingleItem();
    }

    [TestMethod]
    public void SC010_Registration_ChangeTickets_SameSelection_StillRaisesEvent()
    {
        var sut = NewRegistration();
        ClearEvents(sut);
        var sameTickets = new List<TicketTypeSnapshot>
        {
            new("general-admission", "General Admission", [])
        };

        sut.ChangeTickets(sameTickets, DateTimeOffset.UtcNow);

        sut.Tickets.Count.ShouldBe(1);
        sut.GetDomainEvents().OfType<TicketsChangedDomainEvent>().ShouldHaveSingleItem();
    }

    [TestMethod]
    public void SC011_Registration_ChangeTickets_Cancelled_Throws()
    {
        var sut = NewRegistration();
        sut.Cancel(CancellationReason.AttendeeRequest);

        var result = ErrorResult.Capture(() =>
            sut.ChangeTickets([new("workshop", "Workshop", [])], DateTimeOffset.UtcNow));

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
            [new TicketTypeSnapshot("workshop", "Workshop", ["morning"])],
            AdditionalDetails.From(new Dictionary<string, string> { ["tshirt"] = "M" }));

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
            [new TicketTypeSnapshot("workshop", "Workshop", [])],
            AdditionalDetails.Empty));

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
            [new TicketTypeSnapshot("workshop", "Workshop", [])],
            AdditionalDetails.Empty);

        sut.CancellationReason.ShouldBeNull();
        sut.HasReconfirmed.ShouldBeFalse();
        sut.ReconfirmedAt.ShouldBeNull();
    }

    [TestMethod]
    public void Reset_CancelledRegistration_ReplacesAttendeeTicketsAndAdditionalDetails()
    {
        var sut = Registration.Create(
            DefaultTeamId,
            DefaultEventId,
            DefaultEmail,
            FirstName.From("Old"),
            LastName.From("Name"),
            [new TicketTypeSnapshot("old-ticket", "Old Ticket", ["old-slot"])],
            AdditionalDetails.From(new Dictionary<string, string> { ["meal"] = "vegan" }));
        sut.Cancel(CancellationReason.AttendeeRequest);

        sut.Reset(
            FirstName.From("New"),
            LastName.From("Person"),
            [
                new TicketTypeSnapshot("workshop", "Workshop", ["morning"]),
                new TicketTypeSnapshot("dinner", "Dinner", [])
            ],
            AdditionalDetails.From(new Dictionary<string, string> { ["tshirt"] = "M" }));

        sut.FirstName.ShouldBe(FirstName.From("New"));
        sut.LastName.ShouldBe(LastName.From("Person"));
        sut.Tickets.Count.ShouldBe(2);
        sut.Tickets.ShouldContain(t => t.Slug == "workshop" && t.Name == "Workshop");
        sut.Tickets.ShouldContain(t => t.Slug == "dinner" && t.Name == "Dinner");
        sut.AdditionalDetails.Count.ShouldBe(1);
        sut.AdditionalDetails["tshirt"].ShouldBe("M");
    }

    [TestMethod]
    public void Reset_CancelledRegistration_RaisesAttendeeRegisteredDomainEventWithCurrentData()
    {
        var sut = NewRegistration();
        sut.Cancel(CancellationReason.AttendeeRequest);
        ClearEvents(sut);
        var tickets = new List<TicketTypeSnapshot>
        {
            new("workshop", "Workshop", ["morning"])
        };

        sut.Reset(
            FirstName.From("Reset"),
            LastName.From("User"),
            tickets,
            AdditionalDetails.Empty);

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
    }

    private static Registration NewRegistration() =>
        Registration.Create(
            DefaultTeamId,
            DefaultEventId,
            DefaultEmail,
            DefaultFirstName,
            DefaultLastName,
            [new TicketTypeSnapshot("general-admission", "general-admission", [])]);

    private static void ClearEvents(Registration r) => r.ClearDomainEvents();
}
