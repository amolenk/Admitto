using Amolenk.Admitto.Core.Badges.Application.UseCases.EventLifecycle.ArchiveBadgesEvent;
using Amolenk.Admitto.Core.Badges.Application.UseCases.EventLifecycle.ArchiveBadgesEvent.EventHandlers;
using Amolenk.Admitto.Core.Badges.Application.UseCases.EventLifecycle.CreateBadgesEvent;
using Amolenk.Admitto.Core.Badges.Application.UseCases.EventLifecycle.CreateBadgesEvent.EventHandlers;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.EventLifecycle;

[TestClass]
public sealed class BadgesEventLifecycleTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask TicketedEventCreated_CreatesActiveBadgesEvent()
    {
        var integrationEvent = new TicketedEventCreatedIntegrationEvent(
            CreationRequestId: Guid.NewGuid(),
            TeamId: Guid.NewGuid(),
            TicketedEventId: Guid.NewGuid(),
            TimeZone: "UTC");

        var handler = new TicketedEventCreatedIntegrationEventHandler(
            new CreateBadgesEventHandler(Environment.BadgesDatabase.Context));

        await handler.HandleAsync(integrationEvent, testContext.CancellationToken);
        await Environment.BadgesDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await Environment.BadgesDatabase.AssertAsync(async ctx =>
        {
            var eventId = TicketedEventId.From(integrationEvent.TicketedEventId);
            var badgesEvent = await ctx.BadgesEvents
                .FirstOrDefaultAsync(e => e.Id == eventId, testContext.CancellationToken);

            badgesEvent.ShouldNotBeNull();
            badgesEvent.Status.ShouldBe(BadgeEventStatus.Active);
        });
    }

    [TestMethod]
    public async ValueTask TicketedEventCreated_IsIdempotentOnRedelivery()
    {
        var integrationEvent = new TicketedEventCreatedIntegrationEvent(
            CreationRequestId: Guid.NewGuid(),
            TeamId: Guid.NewGuid(),
            TicketedEventId: Guid.NewGuid(),
            TimeZone: "UTC");

        var createHandler = new CreateBadgesEventHandler(Environment.BadgesDatabase.Context);
        var handler = new TicketedEventCreatedIntegrationEventHandler(createHandler);

        // First delivery
        await handler.HandleAsync(integrationEvent, testContext.CancellationToken);
        await Environment.BadgesDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        // Second delivery (same integration event ID) — EF idempotency guard on CommandId prevents duplicate
        await handler.HandleAsync(integrationEvent, testContext.CancellationToken);

        await Environment.BadgesDatabase.AssertAsync(async ctx =>
        {
            var eventId = TicketedEventId.From(integrationEvent.TicketedEventId);
            var count = await ctx.BadgesEvents.CountAsync(e => e.Id == eventId, testContext.CancellationToken);
            count.ShouldBe(1);
        });
    }

    [TestMethod]
    public async ValueTask TicketedEventArchived_TransitionsBadgesEventToArchived()
    {
        // Arrange: create event first
        var ticketedEventId = Guid.NewGuid();
        var createEvent = new TicketedEventCreatedIntegrationEvent(
            CreationRequestId: Guid.NewGuid(),
            TeamId: Guid.NewGuid(),
            TicketedEventId: ticketedEventId,
            TimeZone: "UTC");

        var createHandler = new TicketedEventCreatedIntegrationEventHandler(
            new CreateBadgesEventHandler(Environment.BadgesDatabase.Context));
        await createHandler.HandleAsync(createEvent, testContext.CancellationToken);
        await Environment.BadgesDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        // Act: archive
        var archiveEvent = new TicketedEventArchivedIntegrationEvent(
            TeamId: Guid.NewGuid(),
            TicketedEventId: ticketedEventId);

        var archiveHandler = new TicketedEventArchivedIntegrationEventHandler(
            new ArchiveBadgesEventHandler(Environment.BadgesDatabase.Context));
        await archiveHandler.HandleAsync(archiveEvent, testContext.CancellationToken);
        await Environment.BadgesDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        // Assert
        await Environment.BadgesDatabase.AssertAsync(async ctx =>
        {
            var eventId = TicketedEventId.From(ticketedEventId);
            var badgesEvent = await ctx.BadgesEvents
                .FirstOrDefaultAsync(e => e.Id == eventId, testContext.CancellationToken);

            badgesEvent.ShouldNotBeNull();
            badgesEvent.Status.ShouldBe(BadgeEventStatus.Archived);
        });
    }

    [TestMethod]
    public async ValueTask TicketedEventArchived_IsSafeWhenBadgesEventDoesNotExist()
    {
        // Archive handler uses null-safe ?.MarkArchived() — must not throw for unknown event
        var archiveEvent = new TicketedEventArchivedIntegrationEvent(
            TeamId: Guid.NewGuid(),
            TicketedEventId: Guid.NewGuid());

        var archiveHandler = new TicketedEventArchivedIntegrationEventHandler(
            new ArchiveBadgesEventHandler(Environment.BadgesDatabase.Context));

        // Should complete without exception
        await archiveHandler.HandleAsync(archiveEvent, testContext.CancellationToken);
        await Environment.BadgesDatabase.Context.SaveChangesAsync(testContext.CancellationToken);
    }
}
