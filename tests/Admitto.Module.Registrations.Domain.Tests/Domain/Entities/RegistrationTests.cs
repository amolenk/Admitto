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
    public void SC002_Registration_Create_DuplicateSlugs_ThrowsBusinessRuleViolation()
    {
        // Arrange
        var duplicateSlug = "general-admission";

        var tickets = new List<TicketTypeSnapshot>
        {
            new(duplicateSlug, duplicateSlug, []),
            new(duplicateSlug, duplicateSlug, [])
        };

        // Act
        var result = ErrorResult.Capture(() =>
            Registration.Create(DefaultTeamId, DefaultEventId, DefaultEmail, DefaultFirstName, DefaultLastName, tickets));

        // Assert
        result.Error.ShouldMatch(Registration.Errors.DuplicateTicketTypes([duplicateSlug]));
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