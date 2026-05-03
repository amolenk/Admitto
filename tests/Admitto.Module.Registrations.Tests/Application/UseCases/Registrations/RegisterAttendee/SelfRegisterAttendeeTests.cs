using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.RegisterAttendee;
using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.RegisterAttendee;

[TestClass]
public sealed class SelfRegisterAttendeeTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC001: Successful self-service registration
    [TestMethod]
    public async ValueTask SC001_SelfRegisterAttendee_Success_CreatesRegistrationAndUpdatesCapacity()
    {
        var fixture = RegisterAttendeeFixture.OpenWindowWithCapacity(max: 100, used: 50);
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", fixture.TicketTypeSlug);
        var sut = NewHandler();

        var registrationId = await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Id.ShouldBe(registrationId);
            registration.Email.Value.ShouldBe("dave@example.com");
            registration.Tickets.ShouldHaveSingleItem().Slug.ShouldBe(fixture.TicketTypeSlug);

            var catalog = await dbContext.TicketCatalogs.SingleOrDefaultAsync(testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes[0].UsedCapacity.ShouldBe(51);
        });
    }

    // SC002: Self-service rejected — capacity full
    [TestMethod]
    public async ValueTask SC002_SelfRegisterAttendee_CapacityFull_ThrowsAtCapacityError()
    {
        var fixture = RegisterAttendeeFixture.CapacityFull();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", "workshop");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_type.at_capacity");
    }

    // SC003: Self-service rejected — ticket type has no capacity set
    [TestMethod]
    public async ValueTask SC003_SelfRegisterAttendee_NoCapacitySet_ThrowsNotAvailableError()
    {
        var fixture = RegisterAttendeeFixture.NoCapacitySet();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", "speaker-pass");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_type.not_available");
    }

    // SC004: Self-service rejected — before registration window opens
    [TestMethod]
    public async ValueTask SC004_SelfRegisterAttendee_BeforeWindowOpens_ThrowsRegistrationNotOpen()
    {
        var fixture = RegisterAttendeeFixture.WindowNotYetOpen();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", "general-admission");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.not_open");
    }

    // SC005: Self-service rejected — after registration window closes
    [TestMethod]
    public async ValueTask SC005_SelfRegisterAttendee_AfterWindowCloses_ThrowsRegistrationClosed()
    {
        var fixture = RegisterAttendeeFixture.WindowClosed();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", "general-admission");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.closed");
    }

    // SC006: Self-service rejected — no registration window configured
    [TestMethod]
    public async ValueTask SC006_SelfRegisterAttendee_NoRegistrationPolicy_ThrowsRegistrationNotOpen()
    {
        var fixture = RegisterAttendeeFixture.WithoutRegistrationPolicy();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", "general-admission");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.not_open");
    }

    // SC007: Self-service rejected — email domain mismatch
    [TestMethod]
    public async ValueTask SC007_SelfRegisterAttendee_DomainMismatch_ThrowsEmailDomainNotAllowed()
    {
        var fixture = RegisterAttendeeFixture.WithEmailDomainRestriction("@acme.com");
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "outsider@gmail.com", "general-admission");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.email_domain_not_allowed");
    }

    // SC008: Self-service allowed — email domain matches
    [TestMethod]
    public async ValueTask SC008_SelfRegisterAttendee_DomainMatches_CreatesRegistration()
    {
        var fixture = RegisterAttendeeFixture.WithEmailDomainRestriction("@acme.com");
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "employee@acme.com", "general-admission");
        var sut = NewHandler();

        var registrationId = await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Id.ShouldBe(registrationId);
            registration.Email.Value.ShouldBe("employee@acme.com");
        });
    }

    // SC009: Successful registration with multiple ticket types
    [TestMethod]
    public async ValueTask SC009_SelfRegisterAttendee_MultipleTickets_CreatesRegistrationWithBothTickets()
    {
        var fixture = RegisterAttendeeFixture.WithMultipleTicketTypes();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", "general-admission", "workshop-a");
        var sut = NewHandler();

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleOrDefaultAsync(testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.Tickets.Count.ShouldBe(2);

            var catalog = await dbContext.TicketCatalogs.SingleOrDefaultAsync(testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.TicketTypes.Single(tt => tt.Id == "general-admission").UsedCapacity.ShouldBe(1);
            catalog.TicketTypes.Single(tt => tt.Id == "workshop-a").UsedCapacity.ShouldBe(1);
        });
    }

    // SC010: Rejected — duplicate ticket types in selection
    [TestMethod]
    public async ValueTask SC010_SelfRegisterAttendee_DuplicateTickets_ThrowsDuplicateError()
    {
        var fixture = RegisterAttendeeFixture.OpenWindowWithCapacity();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com",
            fixture.TicketTypeSlug, fixture.TicketTypeSlug);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_catalog.duplicate_ticket_types");
    }

    // SC011: Rejected — non-existent ticket type
    [TestMethod]
    public async ValueTask SC011_SelfRegisterAttendee_UnknownTicketType_ThrowsUnknownTicketTypesError()
    {
        var fixture = RegisterAttendeeFixture.OpenWindowWithCapacity();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", "premium-vip");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_catalog.unknown_ticket_types");
    }

    // SC012: Rejected — cancelled ticket type
    [TestMethod]
    public async ValueTask SC012_SelfRegisterAttendee_CancelledTicketType_ThrowsCancelledError()
    {
        var fixture = RegisterAttendeeFixture.WithCancelledTicketType();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", "workshop-a");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_catalog.cancelled_ticket_types");
    }

    // SC013: Rejected — overlapping time slots
    [TestMethod]
    public async ValueTask SC013_SelfRegisterAttendee_OverlappingTimeSlots_ThrowsOverlappingError()
    {
        var fixture = RegisterAttendeeFixture.WithOverlappingTimeSlots();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", "workshop-a", "workshop-b");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_catalog.overlapping_time_slots");
    }

    // SC014: Rejected — TicketedEvent status is Cancelled
    [TestMethod]
    public async ValueTask SC014_SelfRegisterAttendee_EventCancelled_ThrowsEventNotActive()
    {
        var fixture = RegisterAttendeeFixture.EventCancelled();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", "general-admission");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.event_not_active");
    }

    // SC014b: Rejected — TicketedEvent status is Archived
    [TestMethod]
    public async ValueTask SC014_SelfRegisterAttendee_EventArchived_ThrowsEventNotActive()
    {
        var fixture = RegisterAttendeeFixture.EventArchived();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", "general-admission");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.event_not_active");
    }

    // SC015: Rejected — TicketCatalog.EventStatus catches concurrent transition
    [TestMethod]
    public async ValueTask SC015_SelfRegisterAttendee_ConcurrentCancelAtClaim_ThrowsEventNotActive()
    {
        var fixture = RegisterAttendeeFixture.ConcurrentCancelDetectedAtClaim();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", "general-admission");
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

    // SC016: Rejected — duplicate active email
    [TestMethod]
    public async ValueTask SC016_SelfRegisterAttendee_DuplicateActiveEmail_ThrowsBusinessConflict()
    {
        var fixture = RegisterAttendeeFixture.WithExistingRegistration();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "alice@example.com", fixture.TicketTypeSlug);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(AlreadyExistsError.Create<Registration>());
    }

    // SC020: Self-service resets a cancelled registration
    [TestMethod]
    public async ValueTask SC020_SelfRegisterAttendee_CancelledRegistration_ResetsExistingRegistration()
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
            [fixture.TicketTypeSlug],
            new Dictionary<string, string> { ["tshirt"] = "M" });
        var sut = NewHandler();

        var registrationId = await sut.HandleAsync(command, testContext.CancellationToken);

        registrationId.ShouldBe(fixture.ExistingRegistrationId);
        await Environment.Database.AssertAsync(async dbContext =>
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
            registration.Tickets.ShouldHaveSingleItem().Slug.ShouldBe(fixture.TicketTypeSlug);
            registration.AdditionalDetails["tshirt"].ShouldBe("M");
            AssertAttendeeRegisteredEvent(registration);

            var catalog = await dbContext.TicketCatalogs.SingleAsync(testContext.CancellationToken);
            catalog.TicketTypes.Single(tt => tt.Id == fixture.TicketTypeSlug).UsedCapacity.ShouldBe(1);
        });
    }

    // SC021: Reset is not applied when self-service gates fail
    [TestMethod]
    public async ValueTask SC021_SelfRegisterAttendee_ResetGateFails_LeavesCancelledRegistrationAndCapacityUnchanged()
    {
        var fixture = RegisterAttendeeFixture
            .WindowClosed()
            .WithCancelledExistingRegistration(email: "alice@example.com");
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "alice@example.com", "general-admission");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(RegisterAttendeeHandler.Errors.RegistrationClosed);
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleAsync(testContext.CancellationToken);
            registration.Id.ShouldBe(fixture.ExistingRegistrationId);
            registration.Status.ShouldBe(RegistrationStatus.Cancelled);
            registration.CancellationReason.ShouldBe(CancellationReason.AttendeeRequest);

            var catalog = await dbContext.TicketCatalogs.SingleAsync(testContext.CancellationToken);
            catalog.TicketTypes.Single(tt => tt.Id == "general-admission").UsedCapacity.ShouldBe(0);
        });
    }

    // SC017: Rejected — missing email-verification token
    [TestMethod]
    public async ValueTask SC017_SelfRegisterAttendee_TokenMissing_ThrowsEmailVerificationRequired()
    {
        var fixture = RegisterAttendeeFixture.OpenWindowWithCapacity();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", new[] { fixture.TicketTypeSlug }, token: null);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("email.verification_required");
    }

    // SC018: Rejected — invalid email-verification token
    [TestMethod]
    public async ValueTask SC018_SelfRegisterAttendee_TokenInvalid_ThrowsEmailVerificationInvalid()
    {
        var fixture = RegisterAttendeeFixture.OpenWindowWithCapacity();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture, "dave@example.com", new[] { fixture.TicketTypeSlug }, token: "WRONG");
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("email.verification_invalid");
    }

    // SC019: Verification runs before any DB lookup — non-existent event still yields verification error
    [TestMethod]
    public async ValueTask SC019_SelfRegisterAttendee_VerificationFailsBeforeEventLookup()
    {
        // No event seeded.
        var command = new RegisterAttendeeCommand(
            TicketedEventId.New(),
            EmailAddress.From("dave@example.com"),
            FirstName.From("Dave"),
            LastName.From("Doe"),
            ["general-admission"],
            RegistrationMode.SelfService,
            CouponCode: null,
            EmailVerificationToken: null);
        var sut = NewHandler();

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Verification error wins over event_not_found.
        result.Error.Code.ShouldBe("email.verification_required");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static RegisterAttendeeCommand NewCommand(
        RegisterAttendeeFixture fixture,
        string email,
        params string[] ticketTypeSlugs)
        => NewCommand(fixture, email, ticketTypeSlugs, StubEmailVerificationTokenValidator.ValidTokenFor(email));

    private static RegisterAttendeeCommand NewCommand(
        RegisterAttendeeFixture fixture,
        string email,
        string[] ticketTypeSlugs,
        string? token)
        => new(
            fixture.EventId,
            EmailAddress.From(email),
            FirstName.From("Test"),
            LastName.From("User"),
            ticketTypeSlugs,
            RegistrationMode.SelfService,
            CouponCode: null,
            EmailVerificationToken: token);

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
            RegistrationMode.SelfService,
            CouponCode: null,
            EmailVerificationToken: StubEmailVerificationTokenValidator.ValidTokenFor(email),
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

    private static RegisterAttendeeHandler NewHandler()
        => new(Environment.Database.Context, TimeProvider.System, new StubEmailVerificationTokenValidator());
}
