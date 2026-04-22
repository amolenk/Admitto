using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.SelfRegisterAttendee;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.SelfRegisterAttendee;

[TestClass]
public sealed class SelfRegisterAttendeeTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC001: Successful self-service registration
    [TestMethod]
    public async ValueTask SC001_SelfRegisterAttendee_Success_CreatesRegistrationAndUpdatesCapacity()
    {
        var fixture = SelfRegisterAttendeeFixture.OpenWindowWithCapacity(max: 100, used: 50);
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture.EventId, "dave@example.com", fixture.TicketTypeSlug);
        var sut = NewHandler(fixture);

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
        var fixture = SelfRegisterAttendeeFixture.CapacityFull();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture.EventId, "dave@example.com", "workshop");
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_type.at_capacity");
    }

    // SC003: Self-service rejected — ticket type has no capacity set
    [TestMethod]
    public async ValueTask SC003_SelfRegisterAttendee_NoCapacitySet_ThrowsNotAvailableError()
    {
        var fixture = SelfRegisterAttendeeFixture.NoCapacitySet();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture.EventId, "dave@example.com", "speaker-pass");
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("ticket_type.not_available");
    }

    // SC004: Self-service rejected — before registration window opens
    [TestMethod]
    public async ValueTask SC004_SelfRegisterAttendee_BeforeWindowOpens_ThrowsRegistrationNotOpen()
    {
        var fixture = SelfRegisterAttendeeFixture.WindowNotYetOpen();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture.EventId, "dave@example.com", "general-admission");
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.not_open");
    }

    // SC005: Self-service rejected — after registration window closes
    [TestMethod]
    public async ValueTask SC005_SelfRegisterAttendee_AfterWindowCloses_ThrowsRegistrationClosed()
    {
        var fixture = SelfRegisterAttendeeFixture.WindowClosed();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture.EventId, "dave@example.com", "general-admission");
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.closed");
    }

    // SC006: Self-service rejected — no registration window configured
    [TestMethod]
    public async ValueTask SC006_SelfRegisterAttendee_NoRegistrationPolicy_ThrowsRegistrationNotOpen()
    {
        var fixture = SelfRegisterAttendeeFixture.WithoutRegistrationPolicy();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture.EventId, "dave@example.com", "general-admission");
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.not_open");
    }

    // SC007: Self-service rejected — email domain mismatch
    [TestMethod]
    public async ValueTask SC007_SelfRegisterAttendee_DomainMismatch_ThrowsEmailDomainNotAllowed()
    {
        var fixture = SelfRegisterAttendeeFixture.WithEmailDomainRestriction("@acme.com");
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture.EventId, "outsider@gmail.com", "general-admission");
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.email_domain_not_allowed");
    }

    // SC008: Self-service allowed — email domain matches
    [TestMethod]
    public async ValueTask SC008_SelfRegisterAttendee_DomainMatches_CreatesRegistration()
    {
        var fixture = SelfRegisterAttendeeFixture.WithEmailDomainRestriction("@acme.com");
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture.EventId, "employee@acme.com", "general-admission");
        var sut = NewHandler(fixture);

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
        var fixture = SelfRegisterAttendeeFixture.WithMultipleTicketTypes();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture.EventId, "dave@example.com", "general-admission", "workshop-a");
        var sut = NewHandler(fixture);

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
        var fixture = SelfRegisterAttendeeFixture.OpenWindowWithCapacity();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture.EventId, "dave@example.com",
            fixture.TicketTypeSlug, fixture.TicketTypeSlug);
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.duplicate_ticket_types");
    }

    // SC011: Rejected — non-existent ticket type
    [TestMethod]
    public async ValueTask SC011_SelfRegisterAttendee_UnknownTicketType_ThrowsUnknownTicketTypesError()
    {
        var fixture = SelfRegisterAttendeeFixture.OpenWindowWithCapacity();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture.EventId, "dave@example.com", "premium-vip");
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.unknown_ticket_types");
    }

    // SC012: Rejected — cancelled ticket type
    [TestMethod]
    public async ValueTask SC012_SelfRegisterAttendee_CancelledTicketType_ThrowsCancelledError()
    {
        var fixture = SelfRegisterAttendeeFixture.WithCancelledTicketType();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture.EventId, "dave@example.com", "workshop-a");
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.cancelled_ticket_types");
    }

    // SC013: Rejected — overlapping time slots
    [TestMethod]
    public async ValueTask SC013_SelfRegisterAttendee_OverlappingTimeSlots_ThrowsOverlappingError()
    {
        var fixture = SelfRegisterAttendeeFixture.WithOverlappingTimeSlots();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture.EventId, "dave@example.com", "workshop-a", "workshop-b");
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.overlapping_time_slots");
    }

    // SC014: Rejected — TicketedEvent status is Cancelled
    [TestMethod]
    public async ValueTask SC014_SelfRegisterAttendee_EventCancelled_ThrowsEventNotActive()
    {
        var fixture = SelfRegisterAttendeeFixture.EventCancelled();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture.EventId, "dave@example.com", "general-admission");
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.event_not_active");
    }

    // SC014b: Rejected — TicketedEvent status is Archived
    [TestMethod]
    public async ValueTask SC014_SelfRegisterAttendee_EventArchived_ThrowsEventNotActive()
    {
        var fixture = SelfRegisterAttendeeFixture.EventArchived();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture.EventId, "dave@example.com", "general-admission");
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.event_not_active");
    }

    // SC015: Rejected — TicketCatalog.EventStatus catches concurrent transition
    [TestMethod]
    public async ValueTask SC015_SelfRegisterAttendee_ConcurrentCancelAtClaim_ThrowsEventNotActive()
    {
        var fixture = SelfRegisterAttendeeFixture.ConcurrentCancelDetectedAtClaim();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture.EventId, "dave@example.com", "general-admission");
        var sut = NewHandler(fixture);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.Code.ShouldBe("registration.event_not_active");

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs.SingleOrDefaultAsync(testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            // No capacity consumed.
            catalog.TicketTypes[0].UsedCapacity.ShouldBe(0);
        });
    }

    // SC016: Rejected — duplicate email (DB constraint)
    [TestMethod]
    public async ValueTask SC016_SelfRegisterAttendee_DuplicateEmail_ThrowsDbConstraintViolation()
    {
        var fixture = SelfRegisterAttendeeFixture.WithExistingRegistration();
        await fixture.SetupAsync(Environment);

        var command = NewCommand(fixture.EventId, "alice@example.com", fixture.TicketTypeSlug);
        var sut = NewHandler(fixture);

        // The handler succeeds (adds entity to context), but SaveChanges fires the unique constraint.
        await sut.HandleAsync(command, testContext.CancellationToken);

        var exception = Should.Throw<DbUpdateException>(
            () => Environment.Database.Context.SaveChangesAsync(testContext.CancellationToken));

        exception.InnerException
            .ShouldBeAssignableTo<PostgresException>()?
            .ConstraintName.ShouldBe("IX_registrations_event_id_email");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static SelfRegisterAttendeeCommand NewCommand(
        TicketedEventId eventId,
        string email,
        params string[] ticketTypeSlugs)
        => new(eventId, EmailAddress.From(email), ticketTypeSlugs);

    private static SelfRegisterAttendeeHandler NewHandler(SelfRegisterAttendeeFixture fixture)
        => new(Environment.Database.Context, TimeProvider.System);
}
