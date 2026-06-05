using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RegisterAttendeeSelfService;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.RegisterAttendee;

[TestClass]
public sealed class SelfRegisterAttendeeTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Successful self-service registration
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_Success_CreatesRegistrationAndUpdatesCapacity()
    {
        var fixture = RegisterAttendeeFixture.OpenWindowWithCapacity(max: 100, used: 50);
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", fixture.TicketTypeId.Value);
        var sut = NewHandler();

        var registrationId = await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Id.Value.ShouldBe(registrationId);
            registration.Email.Value.ShouldBe("dave@example.com");
            registration.Tickets.ShouldHaveSingleItem().Id.ShouldBe(fixture.TicketTypeId);

            var catalog = await dbContext.TicketCatalogs.SingleOrDefaultAsync(testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes[0].UsedCapacity.ShouldBe(51);
        });
    }

    // Self-service rejected — capacity full
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_CapacityFull_ThrowsAtCapacityError()
    {
        var fixture = RegisterAttendeeFixture.CapacityFull();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", fixture.GetTicketTypeId("workshop").Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_type.at_capacity");
    }

    // Self-service rejected — ticket type has self-service disabled
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_SelfServiceDisabled_ThrowsNotAvailableError()
    {
        var fixture = RegisterAttendeeFixture.NoCapacitySet();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", fixture.GetTicketTypeId("speaker-pass").Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_type.not_self_service");
    }

    // Self-service rejected — before registration window opens
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_BeforeWindowOpens_ThrowsRegistrationNotOpen()
    {
        var fixture = RegisterAttendeeFixture.WindowNotYetOpen();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", fixture.GetTicketTypeId("general-admission").Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.not_open");
    }

    // Self-service rejected — after registration window closes
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_AfterWindowCloses_ThrowsRegistrationClosed()
    {
        var fixture = RegisterAttendeeFixture.WindowClosed();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", fixture.GetTicketTypeId("general-admission").Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.closed");
    }

    // Self-service rejected — no registration window configured
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_NoRegistrationPolicy_ThrowsRegistrationNotOpen()
    {
        var fixture = RegisterAttendeeFixture.WithoutRegistrationPolicy();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", fixture.GetTicketTypeId("general-admission").Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.not_open");
    }

    // Self-service rejected — email domain mismatch
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_DomainMismatch_ThrowsEmailDomainNotAllowed()
    {
        var fixture = RegisterAttendeeFixture.WithEmailDomainRestriction("@acme.com");
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "outsider@gmail.com", fixture.GetTicketTypeId("general-admission").Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.email_domain_not_allowed");
    }

    // Self-service allowed — email domain matches
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_DomainMatches_CreatesRegistration()
    {
        var fixture = RegisterAttendeeFixture.WithEmailDomainRestriction("@acme.com");
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "employee@acme.com", fixture.GetTicketTypeId("general-admission").Value);
        var sut = NewHandler();

        var registrationId = await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Id.Value.ShouldBe(registrationId);
            registration.Email.Value.ShouldBe("employee@acme.com");
        });
    }

    // Successful registration with multiple ticket types
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_MultipleTickets_CreatesRegistrationWithBothTickets()
    {
        var fixture = RegisterAttendeeFixture.WithMultipleTicketTypes();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com",
            fixture.GetTicketTypeId("general-admission").Value,
            fixture.GetTicketTypeId("workshop-a").Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Tickets.Count.ShouldBe(2);

            var catalog = await dbContext.TicketCatalogs.SingleOrDefaultAsync(testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes.Single(tt => tt.Id == fixture.GetTicketTypeId("general-admission")).UsedCapacity.ShouldBe(1);
            catalog.TicketTypes.Single(tt => tt.Id == fixture.GetTicketTypeId("workshop-a")).UsedCapacity.ShouldBe(1);
        });
    }

    // Rejected — duplicate ticket types in selection
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_DuplicateTickets_ThrowsDuplicateError()
    {
        var fixture = RegisterAttendeeFixture.OpenWindowWithCapacity();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com",
            fixture.TicketTypeId.Value, fixture.TicketTypeId.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_catalog.duplicate_ticket_types");
    }

    // Rejected — non-existent ticket type
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_UnknownTicketType_ThrowsUnknownTicketTypesError()
    {
        var fixture = RegisterAttendeeFixture.OpenWindowWithCapacity();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", Guid.NewGuid());
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_catalog.unknown_ticket_types");
    }

    // Rejected — overlapping time slots
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_OverlappingTimeSlots_ThrowsOverlappingError()
    {
        var fixture = RegisterAttendeeFixture.WithOverlappingTimeSlots();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com",
            fixture.GetTicketTypeId("workshop-a").Value,
            fixture.GetTicketTypeId("workshop-b").Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_catalog.overlapping_time_slots");
    }

    // Rejected — TicketedEvent status is Archived
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_EventArchived_ThrowsEventNotActive()
    {
        var fixture = RegisterAttendeeFixture.EventArchived();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", fixture.GetTicketTypeId("general-admission").Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.event_not_active");
    }

    // Rejected — TicketCatalog.EventStatus catches concurrent transition
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_ConcurrentCancelAtClaim_ThrowsEventNotActive()
    {
        var fixture = RegisterAttendeeFixture.ConcurrentArchiveDetectedAtClaim();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", fixture.GetTicketTypeId("general-admission").Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_catalog.event_not_active");

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs.SingleOrDefaultAsync(testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes[0].UsedCapacity.ShouldBe(0);
        });
    }

    // Rejected — duplicate active email
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_DuplicateActiveEmail_ThrowsBusinessConflict()
    {
        var fixture = RegisterAttendeeFixture.WithExistingRegistration();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "alice@example.com", fixture.TicketTypeId.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(AlreadyExistsError.Create<Registration>());
    }

    // Self-service resets a cancelled registration
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_CancelledRegistration_ResetsExistingRegistration()
    {
        var fixture = RegisterAttendeeFixture
            .WithAdditionalDetailSchema(("tshirt", "T-shirt size", 5))
            .WithCancelledExistingRegistration(
                email: "alice@example.com",
                additionalDetails: new Dictionary<string, string> { ["tshirt"] = "L" });
        await fixture.SetupAsync(Environment);

        var command = NewCommand(
            fixture,
            "alice@example.com",
            [fixture.TicketTypeId.Value],
            new Dictionary<string, string> { ["tshirt"] = "M" });
        var sut = NewHandler();

        var registrationId = await sut.HandleAsync(command, testContext.CancellationToken);

        registrationId.ShouldBe(fixture.ExistingRegistrationId.Value);
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleAsync(testContext.CancellationToken);
            registration.Id.ShouldBe(fixture.ExistingRegistrationId);
            registration.Status.ShouldBe(RegistrationStatus.Registered);
            registration.Email.Value.ShouldBe("alice@example.com");
            registration.FirstName.ShouldBe(FirstName.From("Test"));
            registration.LastName.ShouldBe(LastName.From("User"));
            registration.CancellationReason.ShouldBeNull();
            registration.HasReconfirmed.ShouldBeFalse();
            registration.ReconfirmedAt.ShouldBeNull();
            registration.Tickets.ShouldHaveSingleItem().Id.ShouldBe(fixture.TicketTypeId);
            registration.AdditionalDetails["tshirt"].ShouldBe("M");
            AssertAttendeeRegisteredEvent(registration);

            var catalog = await dbContext.TicketCatalogs.SingleAsync(testContext.CancellationToken);
            catalog.TicketTypes.Single(tt => tt.Id == fixture.TicketTypeId).UsedCapacity.ShouldBe(1);
        });
    }

    // Reset is not applied when self-service gates fail
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_ResetGateFails_LeavesCancelledRegistrationAndCapacityUnchanged()
    {
        var fixture = RegisterAttendeeFixture
            .WindowClosed()
            .WithCancelledExistingRegistration(email: "alice@example.com");
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "alice@example.com", fixture.GetTicketTypeId("general-admission").Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(TicketedEvent.Errors.RegistrationClosed);
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleAsync(testContext.CancellationToken);
            registration.Id.ShouldBe(fixture.ExistingRegistrationId);
            registration.Status.ShouldBe(RegistrationStatus.Cancelled);
            registration.CancellationReason.ShouldBe(CancellationReason.AttendeeRequest);

            var catalog = await dbContext.TicketCatalogs.SingleAsync(testContext.CancellationToken);
            catalog.TicketTypes.Single(tt => tt.Id == fixture.GetTicketTypeId("general-admission")).UsedCapacity.ShouldBe(0);
        });
    }

    // Self-service rejected — ticket type is in WaitlistMode
    [TestMethod]
    public async ValueTask SelfRegisterAttendee_WaitlistModeActive_ThrowsWaitlistModeError()
    {
        var fixture = RegisterAttendeeFixture.SelfServiceWithWaitlistMode();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", fixture.GetTicketTypeId("general-admission").Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_type.waitlist_mode");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static RegisterAttendeeSelfServiceCommand NewCommand(
        RegisterAttendeeFixture fixture,
        string email,
        params Guid[] ticketTypeIds)
        => new(
            fixture.EventId.Value,
            email,
            "Test",
            "User",
            ticketTypeIds);

    private static RegisterAttendeeSelfServiceCommand NewCommand(
        RegisterAttendeeFixture fixture,
        string email,
        Guid[] ticketTypeIds,
        IReadOnlyDictionary<string, string>? additionalDetails)
        => new(
            fixture.EventId.Value,
            email,
            "Test",
            "User",
            ticketTypeIds,
            AdditionalDetails: additionalDetails);

    private static void AssertAttendeeRegisteredEvent(Registration registration)
    {
        var domainEvent = registration.GetDomainEvents()
            .OfType<AttendeeRegisteredDomainEvent>()
            .ShouldHaveSingleItem();
        domainEvent.RegistrationId.ShouldBe(registration.Id);
        domainEvent.RecipientEmail.ShouldBe(registration.Email);
        domainEvent.FirstName.ShouldBe(registration.FirstName);
        domainEvent.LastName.ShouldBe(registration.LastName);
        domainEvent.Tickets.ShouldBe(registration.Tickets);
    }

    private static RegisterAttendeeSelfServiceHandler NewHandler()
        => new(Environment.RegistrationsDatabase.Context, TimeProvider.System);
}
