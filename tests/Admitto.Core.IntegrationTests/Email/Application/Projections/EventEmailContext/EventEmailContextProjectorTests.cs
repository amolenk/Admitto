using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Email.Application.Jobs;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Amolenk.Admitto.Testing.Builders.Registrations.Contracts;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Projections.EventEmailContext;

[TestClass]
public sealed class EventEmailContextProjectorTests(TestContext testContext) : AspireIntegrationTestBase
{
    private static readonly DateTimeOffset Opens = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Closes = new(2030, 12, 31, 0, 0, 0, TimeSpan.Zero);

    private EventEmailContextProjector CreateProjector(
        IReconfirmPolicyCloseScheduler? closeScheduler = null) =>
        new(Environment.EmailDatabase.Context, closeScheduler);

    // Given a created event with a reconfirm policy including overnight quiet hours
    // When the event is projected
    // Then all policy fields are persisted in the email context view
    [TestMethod]
    public async Task TicketedEventCreated_WithPolicy_ProjectsPolicyFields()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var projector = CreateProjector();

        await projector.HandleAsync(
            new TicketedEventCreatedIntegrationEventBuilder()
                .WithTeamId(teamId.Value)
                .WithTicketedEventId(eventId.Value)
                .WithSelfServiceTicketTypeCount(2)
                .WithReconfirmPolicy(new TicketedEventReconfirmPolicySnapshot(
                    Opens, Closes, 24, new TimeOnly(22), new TimeOnly(8)))
                .Build(),
            testContext.CancellationToken);

        await Environment.EmailDatabase.AssertAsync(async db =>
        {
            var view = await db.EventEmailContexts.SingleAsync(
                c => c.TeamId == teamId && c.TicketedEventId == eventId,
                testContext.CancellationToken);

            view.ReconfirmOpensAt.ShouldBe(Opens);
            view.ReconfirmClosesAt.ShouldBe(Closes);
            view.ReconfirmMinEmailIntervalHours.ShouldBe(24);
            view.ReconfirmQuietHoursStart.ShouldBe(new TimeOnly(22));
            view.ReconfirmQuietHoursEnd.ShouldBe(new TimeOnly(8));
            view.HasCompleteReconfirmPolicy.ShouldBeTrue();
        });
    }

    // Given a policy whose close is not aligned to the hourly trigger
    // When the policy is projected
    // Then a one-shot close evaluation is scheduled at the exact close instant
    [TestMethod]
    public async Task TicketedEventCreated_NonHourPolicyClose_SchedulesTerminalEvaluation()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var closeScheduler = Substitute.For<IReconfirmPolicyCloseScheduler>();
        var projector = CreateProjector(closeScheduler);

        await projector.HandleAsync(
            new TicketedEventCreatedIntegrationEventBuilder()
                .WithTeamId(teamId.Value)
                .WithTicketedEventId(eventId.Value)
                .WithReconfirmPolicy(new TicketedEventReconfirmPolicySnapshot(
                    Opens, Closes, 24, null, null))
                .Build(),
            testContext.CancellationToken);

        await closeScheduler.Received(1).ScheduleAsync(
            eventId,
            Closes,
            Arg.Any<CancellationToken>());
    }

    // Given a created event that has already been projected and saved
    // When the same event is delivered again
    // Then only one projection row remains
    [TestMethod]
    public async Task TicketedEventCreated_DuplicateDelivery_IsIdempotent()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var projector = CreateProjector();
        var integrationEvent = new TicketedEventCreatedIntegrationEventBuilder()
            .WithTeamId(teamId.Value)
            .WithTicketedEventId(eventId.Value)
            .Build();

        await projector.HandleAsync(integrationEvent, testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);
        await projector.HandleAsync(integrationEvent, testContext.CancellationToken);

        await Environment.EmailDatabase.AssertAsync(async db =>
        {
            (await db.EventEmailContexts.CountAsync(
                c => c.TeamId == teamId && c.TicketedEventId == eventId,
                testContext.CancellationToken)).ShouldBe(1);
        });
    }

    // Given a newer details event arrives before an older created event
    // When both events are projected out of order
    // Then the newer details and timezone are retained
    [TestMethod]
    public async Task EventDetails_ArrivesBeforeCreated_PreservesNewerTimezone()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var projector = CreateProjector();

        await projector.HandleAsync(
            new TicketedEventDetailsChangedIntegrationEvent(
                teamId.Value,
                eventId.Value,
                TicketedEventVersion: 2,
                Name: "Renamed",
                WebsiteUrl: "https://renamed.example",
                PublicSlug: "renamed",
                TimeZone: "America/Los_Angeles"),
            testContext.CancellationToken);
        await projector.HandleAsync(
            new TicketedEventCreatedIntegrationEventBuilder()
                .WithTeamId(teamId.Value)
                .WithTicketedEventId(eventId.Value)
                .WithTimeZone("UTC")
                .Build(),
            testContext.CancellationToken);

        await Environment.EmailDatabase.AssertAsync(async db =>
        {
            var view = await db.EventEmailContexts.SingleAsync(
                c => c.TeamId == teamId && c.TicketedEventId == eventId,
                testContext.CancellationToken);
            view.EventName.ShouldBe("Renamed");
            view.TimeZone.ShouldBe("America/Los_Angeles");
            view.TicketedEventVersion.ShouldBe(2u);
        });
    }

    // Given a projected event with a complete reconfirm policy
    // When the policy-cleared event is projected
    // Then every policy field is cleared
    [TestMethod]
    public async Task ReconfirmPolicyChanged_PolicyCleared_ClearsProjectedPolicy()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var projector = CreateProjector();

        await projector.HandleAsync(
            new TicketedEventCreatedIntegrationEventBuilder()
                .WithTeamId(teamId.Value)
                .WithTicketedEventId(eventId.Value)
                .WithReconfirmPolicy(new TicketedEventReconfirmPolicySnapshot(
                    Opens, Closes, 24, new TimeOnly(22), new TimeOnly(8)))
                .Build(),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await projector.HandleAsync(
            new TicketedEventReconfirmPolicyChangedIntegrationEvent(
                teamId.Value, eventId.Value, TicketedEventVersion: 2, Policy: null),
            testContext.CancellationToken);

        await Environment.EmailDatabase.AssertAsync(async db =>
        {
            var view = await db.EventEmailContexts.SingleAsync(
                c => c.TeamId == teamId && c.TicketedEventId == eventId,
                testContext.CancellationToken);
            view.ReconfirmOpensAt.ShouldBeNull();
            view.ReconfirmClosesAt.ShouldBeNull();
            view.ReconfirmMinEmailIntervalHours.ShouldBeNull();
            view.ReconfirmQuietHoursStart.ShouldBeNull();
            view.ReconfirmQuietHoursEnd.ShouldBeNull();
            view.HasCompleteReconfirmPolicy.ShouldBeFalse();
        });
    }

    // Given a fully projected active event
    // When an archive event is projected
    // Then the event is no longer eligible for routine evaluation
    [TestMethod]
    public async Task TicketedEventArchived_MarksViewArchived()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var projector = CreateProjector();

        await projector.HandleAsync(
            new TicketedEventCreatedIntegrationEventBuilder()
                .WithTeamId(teamId.Value)
                .WithTicketedEventId(eventId.Value)
                .WithReconfirmPolicy(new TicketedEventReconfirmPolicySnapshot(
                    Opens, Closes, 24, null, null))
                .Build(),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await projector.HandleAsync(
            new TicketedEventArchivedIntegrationEvent(teamId.Value, eventId.Value, 2),
            testContext.CancellationToken);

        await Environment.EmailDatabase.AssertAsync(async db =>
        {
            var view = await db.EventEmailContexts.SingleAsync(
                c => c.TeamId == teamId && c.TicketedEventId == eventId,
                testContext.CancellationToken);
            view.IsArchived.ShouldBeTrue();
            view.HasCompleteReconfirmPolicy.ShouldBeFalse();
        });
    }
}
