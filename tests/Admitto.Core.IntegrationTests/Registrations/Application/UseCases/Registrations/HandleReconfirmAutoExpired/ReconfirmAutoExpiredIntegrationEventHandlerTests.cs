using Amolenk.Admitto.Core.Email.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.HandleReconfirmAutoExpired.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.HandleReconfirmAutoExpired;

[TestClass]
public sealed class ReconfirmAutoExpiredIntegrationEventHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask HandleAsync_RegisteredUnreconfirmedRegistration_CancelsRegistration()
    {
        var fixture = HandleReconfirmAutoExpiredFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);
        await ClearOutboxAsync();

        var sut = new ReconfirmAutoExpiredIntegrationEventHandler(Environment.RegistrationsDatabase.Context);
        await sut.HandleAsync(
            new ReconfirmAutoExpiredIntegrationEvent(fixture.TicketedEventId.Value, [fixture.RegistrationId.Value]),
            testContext.CancellationToken);
        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var registration = await db.Registrations.FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.Status.ShouldBe(RegistrationStatus.Cancelled);
            registration.CancellationReason.ShouldBe(CancellationReason.ReconfirmAutoCancel);
        });
    }

    [TestMethod]
    public async ValueTask HandleAsync_AlreadyReconfirmedRegistration_SkipsCancellation()
    {
        var fixture = HandleReconfirmAutoExpiredFixture.ReconfirmedRegistration();
        await fixture.SetupAsync(Environment);
        await ClearOutboxAsync();

        var sut = new ReconfirmAutoExpiredIntegrationEventHandler(Environment.RegistrationsDatabase.Context);
        await sut.HandleAsync(
            new ReconfirmAutoExpiredIntegrationEvent(fixture.TicketedEventId.Value, [fixture.RegistrationId.Value]),
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

    [TestMethod]
    public async ValueTask HandleAsync_AlreadyCancelledRegistration_SkipsCancellation()
    {
        var fixture = HandleReconfirmAutoExpiredFixture.CancelledRegistration();
        await fixture.SetupAsync(Environment);
        await ClearOutboxAsync();

        var sut = new ReconfirmAutoExpiredIntegrationEventHandler(Environment.RegistrationsDatabase.Context);
        await sut.HandleAsync(
            new ReconfirmAutoExpiredIntegrationEvent(fixture.TicketedEventId.Value, [fixture.RegistrationId.Value]),
            testContext.CancellationToken);
        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var registration = await db.Registrations.FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.CancellationReason.ShouldBe(CancellationReason.AttendeeRequest);
        });
    }

    [TestMethod]
    public async ValueTask HandleAsync_RedeliveredEvent_IsIdempotent()
    {
        var fixture = HandleReconfirmAutoExpiredFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);
        await ClearOutboxAsync();

        var integrationEventId = Guid.NewGuid();
        var integrationEvent = new ReconfirmAutoExpiredIntegrationEvent(fixture.TicketedEventId.Value, [fixture.RegistrationId.Value])
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

    private static async Task ClearOutboxAsync()
    {
        var db = Environment.RegistrationsDatabase.Context;
        db.OutboxMessages.RemoveRange(db.OutboxMessages);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }
}
