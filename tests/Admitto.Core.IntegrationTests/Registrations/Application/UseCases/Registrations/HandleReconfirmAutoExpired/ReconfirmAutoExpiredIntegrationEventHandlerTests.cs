using Amolenk.Admitto.Core.Email.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.HandleReconfirmAutoExpired.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.HandleReconfirmAutoExpired;

[TestClass]
public sealed class ReconfirmAutoExpiredIntegrationEventHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a registered attendee who has not reconfirmed before the deadline
    // When the reconfirm-auto-expired event is handled
    // Then the registration is cancelled with a reconfirm auto-cancel reason
    [TestMethod]
    public async ValueTask HandleAsync_RegisteredUnreconfirmedRegistration_CancelsRegistration()
    {
        // DatabaseTestContext intentionally omits DomainEventsInterceptor; this handler seam
        // proves the cancellation transition, while dedicated waitlist and email handler tests
        // cover the downstream release/notification paths.
        var fixture = HandleReconfirmAutoExpiredFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);
        await ClearOutboxAsync();

        var sut = new ReconfirmAutoExpiredIntegrationEventHandler(Environment.RegistrationsDatabase.Context);
        await sut.HandleAsync(
            new ReconfirmAutoExpiredIntegrationEvent(fixture.TeamId.Value, fixture.TicketedEventId.Value,
                [],
                [Reference(fixture)]),
            testContext.CancellationToken);
        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var registration = await db.Registrations.FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.Status.ShouldBe(RegistrationStatus.Cancelled);
            registration.CancellationReason.ShouldBe(CancellationReason.ReconfirmAutoCancel);
        });
    }

    // Given an attendee who already reconfirmed their registration
    // When the reconfirm-auto-expired event is handled
    // Then the registration stays registered and no outbox message is produced
    [TestMethod]
    public async ValueTask HandleAsync_AlreadyReconfirmedRegistration_SkipsCancellation()
    {
        var fixture = HandleReconfirmAutoExpiredFixture.ReconfirmedRegistration();
        await fixture.SetupAsync(Environment);
        await ClearOutboxAsync();

        var sut = new ReconfirmAutoExpiredIntegrationEventHandler(Environment.RegistrationsDatabase.Context);
        await sut.HandleAsync(
            new ReconfirmAutoExpiredIntegrationEvent(fixture.TeamId.Value, fixture.TicketedEventId.Value,
                [],
                [Reference(fixture)]),
            testContext.CancellationToken);
        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var registration = await db.Registrations.FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.Status.ShouldBe(RegistrationStatus.Registered);
            registration.HasReconfirmed.ShouldBeTrue();
            (await db.OutboxMessages.ToListAsync(testContext.CancellationToken)).ShouldBeEmpty();
        });
    }

    // Given a registration that was already cancelled for another reason
    // When the reconfirm-auto-expired event is handled
    // Then the original cancellation reason is left unchanged
    [TestMethod]
    public async ValueTask HandleAsync_AlreadyCancelledRegistration_SkipsCancellation()
    {
        var fixture = HandleReconfirmAutoExpiredFixture.CancelledRegistration();
        await fixture.SetupAsync(Environment);
        await ClearOutboxAsync();

        var sut = new ReconfirmAutoExpiredIntegrationEventHandler(Environment.RegistrationsDatabase.Context);
        await sut.HandleAsync(
            new ReconfirmAutoExpiredIntegrationEvent(fixture.TeamId.Value, fixture.TicketedEventId.Value,
                [],
                [Reference(fixture)]),
            testContext.CancellationToken);
        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var registration = await db.Registrations.FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.CancellationReason.ShouldBe(CancellationReason.AttendeeRequest);
        });
    }

    // Given a registration belonging to an archived ticketed event
    // When the reconfirm-auto-expired event is handled
    // Then the registration is left registered but the event is still recorded as processed
    [TestMethod]
    public async ValueTask HandleAsync_ArchivedEvent_SkipsCancellation()
    {
        var fixture = HandleReconfirmAutoExpiredFixture.ArchivedEventRegistration();
        await fixture.SetupAsync(Environment);
        await ClearOutboxAsync();

        var integrationEventId = Guid.NewGuid();
        var integrationEvent = new ReconfirmAutoExpiredIntegrationEvent(fixture.TeamId.Value, fixture.TicketedEventId.Value,
            [],
            [Reference(fixture)])
        {
            IntegrationEventId = integrationEventId
        };

        var sut = new ReconfirmAutoExpiredIntegrationEventHandler(Environment.RegistrationsDatabase.Context);
        await sut.HandleAsync(integrationEvent, testContext.CancellationToken);
        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var registration = await db.Registrations.FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.Status.ShouldBe(RegistrationStatus.Registered);

            var processedMessage = await db.ProcessedMessages.SingleAsync(testContext.CancellationToken);
            processedMessage.MessageKey.ShouldBe(integrationEventId.ToString("N"));
        });
    }

    // Given a reconfirm-auto-expired event that has already been processed once
    // When the same event is handled again
    // Then the registration is cancelled only once and the event is recorded as processed a single time
    [TestMethod]
    public async ValueTask HandleAsync_RedeliveredEvent_IsIdempotent()
    {
        var fixture = HandleReconfirmAutoExpiredFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);
        await ClearOutboxAsync();

        var integrationEventId = Guid.NewGuid();
        var integrationEvent = new ReconfirmAutoExpiredIntegrationEvent(fixture.TeamId.Value, fixture.TicketedEventId.Value,
            [],
            [Reference(fixture)])
        {
            IntegrationEventId = integrationEventId
        };

        var sut = new ReconfirmAutoExpiredIntegrationEventHandler(Environment.RegistrationsDatabase.Context);
        await sut.HandleAsync(integrationEvent, testContext.CancellationToken);
        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        Environment.RegistrationsDatabase.Context.ChangeTracker.Clear();

        sut = new ReconfirmAutoExpiredIntegrationEventHandler(Environment.RegistrationsDatabase.Context);
        await sut.HandleAsync(integrationEvent, testContext.CancellationToken);
        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var registration = await db.Registrations.FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.CancellationReason.ShouldBe(CancellationReason.ReconfirmAutoCancel);

            var processedMessages = await db.ProcessedMessages.ToListAsync(testContext.CancellationToken);
            processedMessages.Count.ShouldBe(1);
            processedMessages[0].MessageKey.ShouldBe(integrationEventId.ToString("N"));
        });
    }

    // Given a reconfirm-auto-expired message without cycle references
    // When the message is handled
    // Then the registration is left unchanged
    [TestMethod]
    public async ValueTask HandleAsync_LegacyMessageWithoutCycleReferences_DoesNotCancel()
    {
        var fixture = HandleReconfirmAutoExpiredFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);
        await ClearOutboxAsync();

        var sut = new ReconfirmAutoExpiredIntegrationEventHandler(Environment.RegistrationsDatabase.Context);
        await sut.HandleAsync(
            new ReconfirmAutoExpiredIntegrationEvent(
                fixture.TeamId.Value,
                fixture.TicketedEventId.Value,
                [fixture.RegistrationId.Value]),
            testContext.CancellationToken);
        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var registration = await db.Registrations.FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.Status.ShouldBe(RegistrationStatus.Registered);
        });
    }

    // Given a reconfirm-auto-expired message for a previous registration cycle
    // When the registration is reset before the message is handled
    // Then the fresh cycle remains registered
    [TestMethod]
    public async ValueTask HandleAsync_StaleCycleAfterReset_DoesNotCancelFreshRegistration()
    {
        var fixture = HandleReconfirmAutoExpiredFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);
        await ClearOutboxAsync();
        var oldCycleId = fixture.CycleId;

        await Environment.RegistrationsDatabase.SeedAsync(db =>
        {
            var registration = db.Registrations.First(r => r.Id == fixture.RegistrationId);
            registration.Cancel(CancellationReason.AttendeeRequest);
            registration.Reset(
                FirstName.From("Reset"),
                LastName.From("User"),
                registration.Tickets,
                registration.AdditionalDetails,
                DateTimeOffset.UtcNow);
        });

        var sut = new ReconfirmAutoExpiredIntegrationEventHandler(Environment.RegistrationsDatabase.Context);
        await sut.HandleAsync(
            new ReconfirmAutoExpiredIntegrationEvent(
                fixture.TeamId.Value,
                fixture.TicketedEventId.Value,
                [],
                [new ReconfirmAutoExpiredRegistrationReference(
                    fixture.RegistrationId.Value,
                    oldCycleId.Value,
                    fixture.RegistrationVersion,
                    fixture.CatalogVersion,
                    [fixture.TicketTypeId.Value])]),
            testContext.CancellationToken);
        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var registration = await db.Registrations.FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.Status.ShouldBe(RegistrationStatus.Registered);
            registration.RegistrationCycleId.ShouldNotBe(oldCycleId);
        });
    }

    // Given a reconfirm-auto-expired message with the original ticket snapshot
    // When the registration tickets change before the message is handled
    // Then the registration remains registered
    [TestMethod]
    public async ValueTask HandleAsync_TicketChangeAfterEvaluation_DoesNotCancel()
    {
        var fixture = HandleReconfirmAutoExpiredFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);
        await ClearOutboxAsync();
        var reference = Reference(fixture);

        await Environment.RegistrationsDatabase.SeedAsync(db =>
        {
            var registration = db.Registrations.First(r => r.Id == fixture.RegistrationId);
            registration.ChangeTickets(
                [new TicketTypeSnapshot(TicketTypeId.New(), TicketTypeName.From("Changed"), [])],
                DateTimeOffset.UtcNow);
        });

        var sut = new ReconfirmAutoExpiredIntegrationEventHandler(Environment.RegistrationsDatabase.Context);
        await sut.HandleAsync(
            new ReconfirmAutoExpiredIntegrationEvent(
                fixture.TeamId.Value,
                fixture.TicketedEventId.Value,
                [],
                [reference]),
            testContext.CancellationToken);
        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            (await db.Registrations.FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken))
                .Status.ShouldBe(RegistrationStatus.Registered);
        });
    }

    // Given a reconfirm-auto-expired message with the current catalog version
    // When the ticket-type reconfirmation limit changes before handling
    // Then the registration remains registered for the next evaluation
    [TestMethod]
    public async ValueTask HandleAsync_TicketCatalogVersionChanged_DoesNotCancel()
    {
        var fixture = HandleReconfirmAutoExpiredFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);
        await ClearOutboxAsync();
        var reference = Reference(fixture);

        await Environment.RegistrationsDatabase.SeedAsync(db =>
        {
            var catalog = db.TicketCatalogs.First(c => c.Id == fixture.TicketedEventId);
            catalog.UpdateTicketType(
                fixture.TicketTypeId,
                null,
                null,
                maxReconfirmationEmails: ReconfirmationEmailLimit.From(2),
                updateMaxReconfirmationEmails: true);
        });

        var sut = new ReconfirmAutoExpiredIntegrationEventHandler(Environment.RegistrationsDatabase.Context);
        await sut.HandleAsync(
            new ReconfirmAutoExpiredIntegrationEvent(
                fixture.TeamId.Value,
                fixture.TicketedEventId.Value,
                [],
                [reference]),
            testContext.CancellationToken);
        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            (await db.Registrations.FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken))
                .Status.ShouldBe(RegistrationStatus.Registered);
        });
    }

    private static async Task ClearOutboxAsync()
    {
        var db = Environment.RegistrationsDatabase.Context;
        db.OutboxMessages.RemoveRange(db.OutboxMessages);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static ReconfirmAutoExpiredRegistrationReference Reference(
        HandleReconfirmAutoExpiredFixture fixture) =>
        new(
            fixture.RegistrationId.Value,
            fixture.CycleId.Value,
            fixture.RegistrationVersion,
            fixture.CatalogVersion,
            [fixture.TicketTypeId.Value]);
}
