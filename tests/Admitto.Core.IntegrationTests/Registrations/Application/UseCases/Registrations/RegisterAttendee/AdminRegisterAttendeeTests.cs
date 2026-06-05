using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.AdminRegisterAttendee;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.RegisterAttendee;

[TestClass]
public sealed class AdminRegisterAttendeeTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Successful admin-add registration (capacity at limit still allowed)
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_Success_CreatesRegistrationAndIncrementsCapacity()
    {
        var fixture = RegisterAttendeeFixture.OpenWindowWithCapacity(max: 5, used: 5);
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", fixture.TicketTypeId.Value);
        var sut = NewHandler();

        var registrationId = await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Id.Value.ShouldBe(registrationId);
            registration.Email.Value.ShouldBe("speaker@example.com");
            registration.Tickets.ShouldHaveSingleItem().Id.ShouldBe(fixture.TicketTypeId);

            var catalog = await dbContext.TicketCatalogs.SingleOrDefaultAsync(testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes[0].UsedCapacity.ShouldBe(6);
        });
    }

    // Admin-add bypasses registration window — before opens
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_BeforeWindowOpens_CreatesRegistration()
    {
        var fixture = RegisterAttendeeFixture.WindowNotYetOpen();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", fixture.GetTicketTypeId("general-admission").Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await AssertSingleRegistrationAsync("speaker@example.com");
    }

    // Admin-add bypasses registration window — already closed
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_AfterWindowCloses_CreatesRegistration()
    {
        var fixture = RegisterAttendeeFixture.WindowClosed();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", fixture.GetTicketTypeId("general-admission").Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await AssertSingleRegistrationAsync("speaker@example.com");
    }

    // Admin-add bypasses registration window — never configured
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_NoRegistrationPolicy_CreatesRegistration()
    {
        var fixture = RegisterAttendeeFixture.WithoutRegistrationPolicy();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", fixture.GetTicketTypeId("general-admission").Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await AssertSingleRegistrationAsync("speaker@example.com");
    }

    // Admin-add bypasses email-domain restriction
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_DomainMismatch_CreatesRegistration()
    {
        var fixture = RegisterAttendeeFixture.WithEmailDomainRestriction("@acme.com");
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "external@gmail.com", fixture.GetTicketTypeId("general-admission").Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await AssertSingleRegistrationAsync("external@gmail.com");
    }

    // Admin-add bypasses capacity limit
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_CapacityFull_CreatesRegistrationAndExceedsLimit()
    {
        var fixture = RegisterAttendeeFixture.CapacityFull();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", fixture.GetTicketTypeId("workshop").Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs.SingleOrDefaultAsync(testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes[0].UsedCapacity.ShouldBe(21);
        });
    }

    // Admin-add bypasses missing capacity configuration
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_NoCapacitySet_CreatesRegistration()
    {
        var fixture = RegisterAttendeeFixture.NoCapacitySet();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", fixture.GetTicketTypeId("speaker-pass").Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await AssertSingleRegistrationAsync("speaker@example.com");
    }

    // Admin-add rejected — event not active (Archived)
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_EventArchived_ThrowsEventNotActive()
    {
        var fixture = RegisterAttendeeFixture.EventArchived();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", fixture.GetTicketTypeId("general-admission").Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.event_not_active");
    }

    // Admin-add rejected — event not found
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_EventNotFound_ThrowsEventNotFound()
    {
        var fixture = RegisterAttendeeFixture.EventNotFound();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", Guid.NewGuid());
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticketed-event.not_found");
    }

    // Admin-add rejected — no ticket types configured
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_NoTicketCatalog_ThrowsNoTicketTypesConfigured()
    {
        var fixture = RegisterAttendeeFixture.EventWithoutTicketCatalog();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", Guid.NewGuid());
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket-catalog.not_found");
    }

    // Admin-add rejected — duplicate active email
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_DuplicateActiveEmail_ThrowsBusinessConflict()
    {
        var fixture = RegisterAttendeeFixture.WithExistingRegistration();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "alice@example.com", fixture.TicketTypeId.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(AlreadyExistsError.Create<Registration>());
    }

    // Admin-add resets a cancelled registration
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_CancelledRegistration_ResetsExistingRegistration()
    {
        var fixture = RegisterAttendeeFixture
            .OpenWindowWithCapacity(max: 5, used: 5)
            .ConfigureAdditionalDetailSchema(("meal", "Meal", 20))
            .WithCancelledExistingRegistration(
                email: "alice@example.com",
                additionalDetails: new Dictionary<string, string> { ["meal"] = "standard" });
        await fixture.SetupAsync(Environment);

        var command = NewCommand(
            fixture,
            "alice@example.com",
            [fixture.TicketTypeId.Value],
            new Dictionary<string, string> { ["meal"] = "vegan" });
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
            registration.AdditionalDetails["meal"].ShouldBe("vegan");
            AssertAttendeeRegisteredEvent(registration);

            var catalog = await dbContext.TicketCatalogs.SingleAsync(testContext.CancellationToken);
            catalog.TicketTypes.Single(tt => tt.Id == fixture.TicketTypeId).UsedCapacity.ShouldBe(6);
        });
    }

    // Admin-add rejected — duplicate ticket types in selection
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_DuplicateTickets_ThrowsDuplicateError()
    {
        var fixture = RegisterAttendeeFixture.OpenWindowWithCapacity();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com",
            fixture.TicketTypeId.Value, fixture.TicketTypeId.Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_catalog.duplicate_ticket_types");
    }

    // Admin-add rejected — unknown ticket type
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_UnknownTicketType_ThrowsUnknownTicketTypesError()
    {
        var fixture = RegisterAttendeeFixture.OpenWindowWithCapacity();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", Guid.NewGuid());
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_catalog.unknown_ticket_types");
    }

    // Admin-add rejected — overlapping time slots
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_OverlappingTimeSlots_ThrowsOverlappingError()
    {
        var fixture = RegisterAttendeeFixture.WithOverlappingTimeSlots();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com",
            fixture.GetTicketTypeId("workshop-a").Value, fixture.GetTicketTypeId("workshop-b").Value);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_catalog.overlapping_time_slots");
    }

    // Admin-add rejected — additional detail key not in schema
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_UnknownAdditionalDetailKey_ThrowsKeyNotInSchema()
    {
        var fixture = RegisterAttendeeFixture.WithAdditionalDetailSchema(
            ("tshirt", "T-shirt size", 5));
        await fixture.SetupAsync(Environment);

        var command = NewCommand(
            fixture,
            "speaker@example.com",
            [fixture.GetTicketTypeId("general-admission").Value],
            new Dictionary<string, string> { ["shoesize"] = "44" });
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("additional_details.key_not_in_schema");
    }

    // Admin-add rejected — additional detail value too long
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_AdditionalDetailValueTooLong_ThrowsValueTooLong()
    {
        var fixture = RegisterAttendeeFixture.WithAdditionalDetailSchema(
            ("tshirt", "T-shirt size", 5));
        await fixture.SetupAsync(Environment);

        var command = NewCommand(
            fixture,
            "speaker@example.com",
            [fixture.GetTicketTypeId("general-admission").Value],
            new Dictionary<string, string> { ["tshirt"] = "XXXXL-extra-long" });
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("additional_details.value_too_long");
    }

    // Concurrent archive detected at claim time
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_ConcurrentCancelAtClaim_ThrowsEventNotActive()
    {
        var fixture = RegisterAttendeeFixture.ConcurrentArchiveDetectedAtClaim();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", fixture.GetTicketTypeId("general-admission").Value);
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

    // Admin-add does NOT require an email-verification token
    [TestMethod]
    public async ValueTask AdminRegisterAttendee_NoTokenRequired_Succeeds()
    {
        var fixture = RegisterAttendeeFixture.OpenWindowWithCapacity();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", fixture.TicketTypeId.Value);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await AssertSingleRegistrationAsync("speaker@example.com");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async ValueTask AssertSingleRegistrationAsync(string expectedEmail)
    {
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Email.Value.ShouldBe(expectedEmail);
        });
    }

    private static AdminRegisterAttendeeCommand NewCommand(
        RegisterAttendeeFixture fixture,
        string email,
        params Guid[] ticketTypeIds)
        => new(
            fixture.EventId.Value,
            email,
            "Test",
            "User",
            ticketTypeIds);

    private static AdminRegisterAttendeeCommand NewCommand(
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

    private static AdminRegisterAttendeeHandler NewHandler()
        => new(Environment.RegistrationsDatabase.Context);
}
