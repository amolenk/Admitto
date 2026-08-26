using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Amolenk.Admitto.Testing.Builders.Registrations.Contracts;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Projections.EventEmailContext;

[TestClass]
public sealed class EventEmailContextProjectorTests(TestContext testContext) : AspireIntegrationTestBase
{
    private static readonly DateTimeOffset Opens = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Closes = new(2030, 12, 31, 0, 0, 0, TimeSpan.Zero);

    private (EventEmailContextProjector Projector, ICommandHandler<ScheduleReconfirmationsCommand> Schedule) CreateProjector()
    {
        var schedule = Substitute.For<ICommandHandler<ScheduleReconfirmationsCommand>>();
        var projector = new EventEmailContextProjector(Environment.EmailDatabase.Context, schedule);
        return (projector, schedule);
    }

    // Given a TicketedEventCreated integration event that includes a reconfirm policy
    // When the event is projected
    // Then the email context view is upserted with the event details and a reconfirm trigger is scheduled
    [TestMethod]
    public async Task TicketedEventCreated_WithPolicy_UpsertsViewAndSchedulesTrigger()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var (projector, schedule) = CreateProjector();

        await projector.HandleAsync(
            new TicketedEventCreatedIntegrationEventBuilder()
                .WithTeamId(teamId.Value)
                .WithTicketedEventId(eventId.Value)
                .WithSelfServiceTicketTypeCount(2)
                .WithReconfirmPolicy(new TicketedEventReconfirmPolicySnapshot(Opens, Closes, 1, 24))
                .Build(),
            testContext.CancellationToken);

        await Environment.EmailDatabase.AssertAsync(async db =>
        {
            var view = await db.EventEmailContexts.SingleOrDefaultAsync(
                c => c.TeamId == teamId && c.TicketedEventId == eventId,
                testContext.CancellationToken);

            view.ShouldNotBeNull();
            view.EventName.ShouldBe("DevConf");
            view.WebsiteUrl.ShouldBe("https://example.com");
            view.PublicSlug.ShouldBe("devconf");
            view.TimeZone.ShouldBe("UTC");
            view.SelfServiceTicketTypeCount.ShouldBe(2);
            view.HasRequiredRenderingContext.ShouldBeTrue();
            view.HasActiveReconfirmScheduleContext.ShouldBeTrue();
        });

        await schedule.Received(1).HandleAsync(
            Arg.Is<ScheduleReconfirmationsCommand>(c =>
                c != null && c.TicketedEventId == eventId.Value && c.Spec != null),
            Arg.Any<CancellationToken>());
    }

    // Given a TicketedEventCreated integration event with no reconfirm policy
    // When the event is projected
    // Then the email context view is upserted but no reconfirm trigger is scheduled
    [TestMethod]
    public async Task TicketedEventCreated_WithoutPolicy_UpsertsViewButDoesNotSchedule()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var (projector, schedule) = CreateProjector();

        await projector.HandleAsync(
            new TicketedEventCreatedIntegrationEventBuilder()
                .WithTeamId(teamId.Value)
                .WithTicketedEventId(eventId.Value)
                .Build(),
            testContext.CancellationToken);

        await Environment.EmailDatabase.AssertAsync(async db =>
        {
            var view = await db.EventEmailContexts.SingleOrDefaultAsync(
                c => c.TeamId == teamId && c.TicketedEventId == eventId,
                testContext.CancellationToken);
            view.ShouldNotBeNull();
            view.HasActiveReconfirmScheduleContext.ShouldBeFalse();
        });

        await schedule.DidNotReceive().HandleAsync(
            Arg.Any<ScheduleReconfirmationsCommand>(), Arg.Any<CancellationToken>());
    }

    // Given a TicketedEventReconfirmPolicyChanged integration event that clears the policy
    // When the event is projected
    // Then a schedule command is issued to remove the reconfirm trigger
    [TestMethod]
    public async Task ReconfirmPolicyChanged_PolicyCleared_RemovesTrigger()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var (projector, schedule) = CreateProjector();

        await projector.HandleAsync(
            new TicketedEventReconfirmPolicyChangedIntegrationEvent(
                teamId.Value, eventId.Value, TicketedEventVersion: 1, Policy: null),
            testContext.CancellationToken);

        await schedule.Received(1).HandleAsync(
            Arg.Is<ScheduleReconfirmationsCommand>(c =>
                c != null && c.TicketedEventId == eventId.Value && c.Spec == null),
            Arg.Any<CancellationToken>());
    }

    // Given a fully-populated, schedulable email context view for an event
    // When a TicketedEventArchived integration event is projected
    // Then the view is marked archived and its reconfirm trigger is removed
    [TestMethod]
    public async Task TicketedEventArchived_MarksArchivedAndRemovesTrigger()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        // Seed a fully-populated, schedulable view first.
        await Environment.EmailDatabase.SeedAsync(db =>
        {
            var view = EventEmailContextView.CreatePartial(teamId, eventId, DateTimeOffset.UtcNow);
            view.UpdateEventContext(
                0, "DevConf", "https://example.com", "devconf", "UTC", 2,
                new TicketedEventReconfirmPolicySnapshot(Opens, Closes, 1, 24), false, DateTimeOffset.UtcNow);
            db.EventEmailContexts.Add(view);
        }, testContext.CancellationToken);

        var (projector, schedule) = CreateProjector();

        await projector.HandleAsync(
            new TicketedEventArchivedIntegrationEvent(teamId.Value, eventId.Value, TicketedEventVersion: 1),
            testContext.CancellationToken);

        await Environment.EmailDatabase.AssertAsync(async db =>
        {
            var view = await db.EventEmailContexts.SingleAsync(
                c => c.TeamId == teamId && c.TicketedEventId == eventId,
                testContext.CancellationToken);
            view.IsArchived.ShouldBeTrue();
            view.HasActiveReconfirmScheduleContext.ShouldBeFalse();
        });

        await schedule.Received(1).HandleAsync(
            Arg.Is<ScheduleReconfirmationsCommand>(c =>
                c != null && c.TicketedEventId == eventId.Value && c.Spec == null),
            Arg.Any<CancellationToken>());
    }

    // Given a TicketedEventDetailsChanged event that arrives before the TicketedEventCreated event for the same event
    // When both events are projected out of order
    // Then a single view accumulates both updates, with the newer source version winning
    [TestMethod]
    public async Task OutOfOrderDetailsThenCreated_AccumulatesIntoSingleView()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var (projector, _) = CreateProjector();

        // Details arrives before the created event (out of order).
        await projector.HandleAsync(
            new TicketedEventDetailsChangedIntegrationEvent(
                teamId.Value,
                eventId.Value,
                TicketedEventVersion: 2,
                "Renamed",
                "https://renamed.example",
                "renamed",
                "Europe/Amsterdam"),
            testContext.CancellationToken);

        await projector.HandleAsync(
            new TicketedEventCreatedIntegrationEventBuilder()
                .WithTeamId(teamId.Value)
                .WithTicketedEventId(eventId.Value)
                .Build(),
            testContext.CancellationToken);

        await Environment.EmailDatabase.AssertAsync(async db =>
        {
            var views = await db.EventEmailContexts
                .Where(c => c.TeamId == teamId && c.TicketedEventId == eventId)
                .ToListAsync(testContext.CancellationToken);

            // A single row accumulates both updates; the newer source version wins.
            views.Count.ShouldBe(1);
            views[0].EventName.ShouldBe("Renamed");
            views[0].PublicSlug.ShouldBe("renamed");
            views[0].TimeZone.ShouldBe("Europe/Amsterdam");
            views[0].TicketedEventVersion.ShouldBe(2u);
        });
    }

    // Given an existing email context view with an active reconfirm policy
    // When a TicketedEventDetailsChanged event changes the event's time zone
    // Then the projection is updated and the reconfirm trigger is rescheduled for the new time zone
    [TestMethod]
    public async Task TicketedEventDetailsChanged_WithNewTimeZone_UpdatesProjectionAndSchedulesTrigger()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        await Environment.EmailDatabase.SeedAsync(db =>
        {
            var view = EventEmailContextView.CreatePartial(teamId, eventId, DateTimeOffset.UtcNow);
            view.UpdateEventContext(
                1,
                "DevConf",
                "https://example.com",
                "devconf",
                "Europe/Amsterdam",
                2,
                new TicketedEventReconfirmPolicySnapshot(Opens, Closes, 1, 24),
                false,
                DateTimeOffset.UtcNow);
            db.EventEmailContexts.Add(view);
        }, testContext.CancellationToken);

        var (projector, schedule) = CreateProjector();

        await projector.HandleAsync(
            new TicketedEventDetailsChangedIntegrationEvent(
                teamId.Value,
                eventId.Value,
                TicketedEventVersion: 2,
                "Renamed",
                "https://renamed.example",
                "renamed",
                "America/Los_Angeles"),
            testContext.CancellationToken);

        await Environment.EmailDatabase.AssertAsync(async db =>
        {
            var view = await db.EventEmailContexts.SingleAsync(
                c => c.TeamId == teamId && c.TicketedEventId == eventId,
                testContext.CancellationToken);

            view.EventName.ShouldBe("Renamed");
            view.PublicSlug.ShouldBe("renamed");
            view.TimeZone.ShouldBe("America/Los_Angeles");
        });

        await schedule.Received(1).HandleAsync(
            Arg.Is<ScheduleReconfirmationsCommand>(c =>
                c != null
                && c.TicketedEventId == eventId.Value
                && c.Spec != null
                && c.Spec.TimeZone == "America/Los_Angeles"),
            Arg.Any<CancellationToken>());
    }

    // Given a TicketedEventCreated event that has already been projected and saved
    // When the same event is delivered and projected again
    // Then only a single view row exists for the event
    [TestMethod]
    public async Task DuplicateCreatedDelivery_IsIdempotent()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var createdEvent = new TicketedEventCreatedIntegrationEventBuilder()
            .WithTeamId(teamId.Value)
            .WithTicketedEventId(eventId.Value)
            .Build();

        var (projector, _) = CreateProjector();

        await projector.HandleAsync(createdEvent, testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);
        await projector.HandleAsync(createdEvent, testContext.CancellationToken);

        await Environment.EmailDatabase.AssertAsync(async db =>
        {
            var count = await db.EventEmailContexts
                .CountAsync(c => c.TeamId == teamId && c.TicketedEventId == eventId,
                    testContext.CancellationToken);
            count.ShouldBe(1);
        });
    }
}
