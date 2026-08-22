using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeEvents.ArchiveBadgeEvent;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeEvents.ArchiveBadgeEvent.EventHandlers;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeEvents.CreateBadgeEvent;
using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeEvents.CreateBadgeEvent.EventHandlers;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Amolenk.Admitto.Testing.Builders.Registrations.Contracts;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeEvents;

[TestClass]
public sealed class BadgeEventTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a ticketed event created integration event
    // When it is handled
    // Then a badges event is created with active status
    [TestMethod]
    public async ValueTask TicketedEventCreated_CreatesActiveBadgesEvent()
    {
        var integrationEvent = new TicketedEventCreatedIntegrationEventBuilder().Build();

        var handler = new TicketedEventCreatedIntegrationEventHandler(
            new CreateBadgeEventHandler(Environment.BadgesDatabase.Context));

        await handler.HandleAsync(integrationEvent, testContext.CancellationToken);
        await Environment.BadgesDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await Environment.BadgesDatabase.AssertAsync(async ctx =>
        {
            var eventId = TicketedEventId.From(integrationEvent.TicketedEventId);
            var badgesEvent = await ctx.BadgeEvents
                .FirstOrDefaultAsync(e => e.Id == eventId, testContext.CancellationToken);

            badgesEvent.ShouldNotBeNull();
            badgesEvent.Status.ShouldBe(BadgeEventStatus.Active);
        });
    }

    // Given a ticketed event created integration event was already handled once
    // When the same integration event is redelivered and handled again
    // Then only one badges event exists for that ticketed event id
    [TestMethod]
    public async ValueTask TicketedEventCreated_IsIdempotentOnRedelivery()
    {
        var integrationEvent = new TicketedEventCreatedIntegrationEventBuilder().Build();

        var createHandler = new CreateBadgeEventHandler(Environment.BadgesDatabase.Context);
        var handler = new TicketedEventCreatedIntegrationEventHandler(createHandler);

        // First delivery
        await handler.HandleAsync(integrationEvent, testContext.CancellationToken);
        await Environment.BadgesDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        // Second delivery (same integration event ID) — EF idempotency guard on CommandId prevents duplicate
        await handler.HandleAsync(integrationEvent, testContext.CancellationToken);

        await Environment.BadgesDatabase.AssertAsync(async ctx =>
        {
            var eventId = TicketedEventId.From(integrationEvent.TicketedEventId);
            var count = await ctx.BadgeEvents.CountAsync(e => e.Id == eventId, testContext.CancellationToken);
            count.ShouldBe(1);
        });
    }

    // Given an existing badges event created from a ticketed event
    // When a ticketed event archived integration event is handled
    // Then the badges event transitions to archived status
    [TestMethod]
    public async ValueTask TicketedEventArchived_TransitionsBadgesEventToArchived()
    {
        // Arrange: create event first
        var ticketedEventId = Guid.NewGuid();
        var createEvent = new TicketedEventCreatedIntegrationEventBuilder()
            .WithTicketedEventId(ticketedEventId)
            .Build();

        var createHandler = new TicketedEventCreatedIntegrationEventHandler(
            new CreateBadgeEventHandler(Environment.BadgesDatabase.Context));
        await createHandler.HandleAsync(createEvent, testContext.CancellationToken);
        await Environment.BadgesDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        // Act: archive
        var archiveEvent = new TicketedEventArchivedIntegrationEvent(
            TeamId: Guid.NewGuid(),
            TicketedEventId: ticketedEventId,
            TicketedEventVersion: 1);

        var archiveHandler = new TicketedEventArchivedIntegrationEventHandler(
            new ArchiveBadgeEventHandler(Environment.BadgesDatabase.Context));
        await archiveHandler.HandleAsync(archiveEvent, testContext.CancellationToken);
        await Environment.BadgesDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        // Assert
        await Environment.BadgesDatabase.AssertAsync(async ctx =>
        {
            var eventId = TicketedEventId.From(ticketedEventId);
            var badgesEvent = await ctx.BadgeEvents
                .FirstOrDefaultAsync(e => e.Id == eventId, testContext.CancellationToken);

            badgesEvent.ShouldNotBeNull();
            badgesEvent.Status.ShouldBe(BadgeEventStatus.Archived);
        });
    }

    // Given no badges event exists for the given ticketed event id
    // When a ticketed event archived integration event is handled
    // Then it completes without throwing
    [TestMethod]
    public async ValueTask TicketedEventArchived_IsSafeWhenBadgesEventDoesNotExist()
    {
        // Archive handler uses null-safe ?.MarkArchived() — must not throw for unknown event
        var archiveEvent = new TicketedEventArchivedIntegrationEvent(
            TeamId: Guid.NewGuid(),
            TicketedEventId: Guid.NewGuid(),
            TicketedEventVersion: 1);

        var archiveHandler = new TicketedEventArchivedIntegrationEventHandler(
            new ArchiveBadgeEventHandler(Environment.BadgesDatabase.Context));

        // Should complete without exception
        await archiveHandler.HandleAsync(archiveEvent, testContext.CancellationToken);
        await Environment.BadgesDatabase.Context.SaveChangesAsync(testContext.CancellationToken);
    }
}
