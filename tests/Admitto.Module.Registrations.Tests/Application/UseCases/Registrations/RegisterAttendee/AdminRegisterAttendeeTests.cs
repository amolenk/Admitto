using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.RegisterAttendee;

[TestClass]
public sealed class AdminRegisterAttendeeTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC001: Successful admin-add registration (capacity at limit still allowed)
    [TestMethod]
    public async ValueTask SC001_AdminRegisterAttendee_Success_CreatesRegistrationAndIncrementsCapacity()
    {
        var fixture = RegisterAttendeeFixture.OpenWindowWithCapacity(max: 5, used: 5);
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", fixture.TicketTypeSlug);
        var sut = NewHandler();

        var registrationId = await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Id.ShouldBe(registrationId);
            registration.Email.Value.ShouldBe("speaker@example.com");
            registration.Tickets.ShouldHaveSingleItem().Slug.ShouldBe(fixture.TicketTypeSlug);

            var catalog = await dbContext.TicketCatalogs.SingleOrDefaultAsync(testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes[0].UsedCapacity.ShouldBe(6);
        });
    }

    // SC002: Admin-add bypasses registration window — before opens
    [TestMethod]
    public async ValueTask SC002_AdminRegisterAttendee_BeforeWindowOpens_CreatesRegistration()
    {
        var fixture = RegisterAttendeeFixture.WindowNotYetOpen();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", "general-admission");
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await AssertSingleRegistrationAsync("speaker@example.com");
    }

    // SC003: Admin-add bypasses registration window — already closed
    [TestMethod]
    public async ValueTask SC003_AdminRegisterAttendee_AfterWindowCloses_CreatesRegistration()
    {
        var fixture = RegisterAttendeeFixture.WindowClosed();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", "general-admission");
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await AssertSingleRegistrationAsync("speaker@example.com");
    }

    // SC004: Admin-add bypasses registration window — never configured
    [TestMethod]
    public async ValueTask SC004_AdminRegisterAttendee_NoRegistrationPolicy_CreatesRegistration()
    {
        var fixture = RegisterAttendeeFixture.WithoutRegistrationPolicy();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", "general-admission");
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await AssertSingleRegistrationAsync("speaker@example.com");
    }

    // SC005: Admin-add bypasses email-domain restriction
    [TestMethod]
    public async ValueTask SC005_AdminRegisterAttendee_DomainMismatch_CreatesRegistration()
    {
        var fixture = RegisterAttendeeFixture.WithEmailDomainRestriction("@acme.com");
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "external@gmail.com", "general-admission");
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await AssertSingleRegistrationAsync("external@gmail.com");
    }

    // SC006: Admin-add bypasses capacity limit
    [TestMethod]
    public async ValueTask SC006_AdminRegisterAttendee_CapacityFull_CreatesRegistrationAndExceedsLimit()
    {
        var fixture = RegisterAttendeeFixture.CapacityFull();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", "workshop");
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs.SingleOrDefaultAsync(testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes[0].UsedCapacity.ShouldBe(21);
        });
    }

    // SC007: Admin-add bypasses missing capacity configuration
    [TestMethod]
    public async ValueTask SC007_AdminRegisterAttendee_NoCapacitySet_CreatesRegistration()
    {
        var fixture = RegisterAttendeeFixture.NoCapacitySet();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", "speaker-pass");
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await AssertSingleRegistrationAsync("speaker@example.com");
    }

    // SC008: Admin-add rejected — event not active (Cancelled)
    [TestMethod]
    public async ValueTask SC008_AdminRegisterAttendee_EventCancelled_ThrowsEventNotActive()
    {
        var fixture = RegisterAttendeeFixture.EventCancelled();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", "general-admission");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.event_not_active");
    }

    // SC009: Admin-add rejected — event not active (Archived)
    [TestMethod]
    public async ValueTask SC009_AdminRegisterAttendee_EventArchived_ThrowsEventNotActive()
    {
        var fixture = RegisterAttendeeFixture.EventArchived();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", "general-admission");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.event_not_active");
    }

    // SC010: Admin-add rejected — event not found
    [TestMethod]
    public async ValueTask SC010_AdminRegisterAttendee_EventNotFound_ThrowsEventNotFound()
    {
        var fixture = RegisterAttendeeFixture.EventNotFound();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", "general-admission");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.event_not_found");
    }

    // SC011: Admin-add rejected — no ticket types configured
    [TestMethod]
    public async ValueTask SC011_AdminRegisterAttendee_NoTicketCatalog_ThrowsNoTicketTypesConfigured()
    {
        var fixture = RegisterAttendeeFixture.EventWithoutTicketCatalog();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", "general-admission");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.no_ticket_types");
    }

    // SC012: Admin-add rejected — duplicate email (DB constraint)
    [TestMethod]
    public async ValueTask SC012_AdminRegisterAttendee_DuplicateEmail_ThrowsDbConstraintViolation()
    {
        var fixture = RegisterAttendeeFixture.WithExistingRegistration();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "alice@example.com", fixture.TicketTypeSlug);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        var exception = Should.Throw<DbUpdateException>(
            () => Environment.Database.Context.SaveChangesAsync(testContext.CancellationToken));

        exception.InnerException
            .ShouldBeAssignableTo<PostgresException>()?
            .ConstraintName.ShouldBe("IX_registrations_event_id_email");
    }

    // SC013: Admin-add rejected — duplicate ticket types in selection
    [TestMethod]
    public async ValueTask SC013_AdminRegisterAttendee_DuplicateTickets_ThrowsDuplicateError()
    {
        var fixture = RegisterAttendeeFixture.OpenWindowWithCapacity();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com",
            fixture.TicketTypeSlug, fixture.TicketTypeSlug);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_catalog.duplicate_ticket_types");
    }

    // SC014: Admin-add rejected — unknown ticket type
    [TestMethod]
    public async ValueTask SC014_AdminRegisterAttendee_UnknownTicketType_ThrowsUnknownTicketTypesError()
    {
        var fixture = RegisterAttendeeFixture.OpenWindowWithCapacity();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", "premium-vip");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_catalog.unknown_ticket_types");
    }

    // SC015: Admin-add rejected — cancelled ticket type
    [TestMethod]
    public async ValueTask SC015_AdminRegisterAttendee_CancelledTicketType_ThrowsCancelledError()
    {
        var fixture = RegisterAttendeeFixture.WithCancelledTicketType();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", "workshop-a");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_catalog.cancelled_ticket_types");
    }

    // SC016: Admin-add rejected — overlapping time slots
    [TestMethod]
    public async ValueTask SC016_AdminRegisterAttendee_OverlappingTimeSlots_ThrowsOverlappingError()
    {
        var fixture = RegisterAttendeeFixture.WithOverlappingTimeSlots();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", "workshop-a", "workshop-b");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_catalog.overlapping_time_slots");
    }

    // SC017: Admin-add rejected — additional detail key not in schema
    [TestMethod]
    public async ValueTask SC017_AdminRegisterAttendee_UnknownAdditionalDetailKey_ThrowsKeyNotInSchema()
    {
        var fixture = RegisterAttendeeFixture.WithAdditionalDetailSchema(
            ("tshirt", "T-shirt size", 5));
        await fixture.SetupAsync(Environment);

        var command = NewCommand(
            fixture,
            "speaker@example.com",
            new[] { "general-admission" },
            new Dictionary<string, string> { ["shoesize"] = "44" });
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("additional_details.key_not_in_schema");
    }

    // SC018: Admin-add rejected — additional detail value too long
    [TestMethod]
    public async ValueTask SC018_AdminRegisterAttendee_AdditionalDetailValueTooLong_ThrowsValueTooLong()
    {
        var fixture = RegisterAttendeeFixture.WithAdditionalDetailSchema(
            ("tshirt", "T-shirt size", 5));
        await fixture.SetupAsync(Environment);

        var command = NewCommand(
            fixture,
            "speaker@example.com",
            new[] { "general-admission" },
            new Dictionary<string, string> { ["tshirt"] = "XXXXL-extra-long" });
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("additional_details.value_too_long");
    }

    // SC019: Concurrent cancel detected at claim time
    [TestMethod]
    public async ValueTask SC019_AdminRegisterAttendee_ConcurrentCancelAtClaim_ThrowsEventNotActive()
    {
        var fixture = RegisterAttendeeFixture.ConcurrentCancelDetectedAtClaim();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "speaker@example.com", "general-admission");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.event_not_active");

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs.SingleOrDefaultAsync(testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes[0].UsedCapacity.ShouldBe(0);
        });
    }

    // SC020: Admin-add does NOT require an email-verification token
    [TestMethod]
    public async ValueTask SC020_AdminRegisterAttendee_NoTokenRequired_Succeeds()
    {
        var fixture = RegisterAttendeeFixture.OpenWindowWithCapacity();
        await fixture.SetupAsync(Environment);

        // Token deliberately omitted; admin mode must not invoke the verifier.
        var command = NewCommand(fixture, "speaker@example.com", fixture.TicketTypeSlug);
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await AssertSingleRegistrationAsync("speaker@example.com");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async ValueTask AssertSingleRegistrationAsync(string expectedEmail)
    {
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Email.Value.ShouldBe(expectedEmail);
        });
    }

    private static RegisterAttendeeCommand NewCommand(
        RegisterAttendeeFixture fixture,
        string email,
        params string[] ticketTypeSlugs)
        => new(
            fixture.EventId,
            EmailAddress.From(email),
            FirstName.From("Test"),
            LastName.From("User"),
            ticketTypeSlugs,
            RegistrationMode.AdminAdd);

    private static RegisterAttendeeCommand NewCommand(
        RegisterAttendeeFixture fixture,
        string email,
        string[] ticketTypeSlugs,
        IReadOnlyDictionary<string, string>? additionalDetails)
        => new(
            fixture.EventId,
            EmailAddress.From(email),
            FirstName.From("Test"),
            LastName.From("User"),
            ticketTypeSlugs,
            RegistrationMode.AdminAdd,
            CouponCode: null,
            EmailVerificationToken: null,
            AdditionalDetails: additionalDetails);

    private static RegisterAttendeeHandler NewHandler()
        => new(Environment.Database.Context, TimeProvider.System, new StubEmailVerificationTokenValidator());
}
