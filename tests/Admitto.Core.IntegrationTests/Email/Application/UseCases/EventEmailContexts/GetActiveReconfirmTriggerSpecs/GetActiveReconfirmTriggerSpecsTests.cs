using Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetActiveReconfirmTriggerSpecs;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Email.Application;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.EventEmailContexts.GetActiveReconfirmTriggerSpecs;

/// <summary>
/// The projection query that decides which events get a reconfirm trigger at
/// all. Reconciliation rebuilds Quartz state from exactly this result, so a row
/// wrongly included or excluded here silently starts or stops reconfirm mail
/// for an event.
/// </summary>
[TestClass]
public sealed class GetActiveReconfirmTriggerSpecsTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask HandleAsync_ActivePolicy_ReturnsSpecMappedFromProjection()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var opensAt = new DateTimeOffset(2030, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var closesAt = new DateTimeOffset(2030, 5, 25, 0, 0, 0, TimeSpan.Zero);

        await SeedAsync(new EventEmailContextViewBuilder()
            .ForTeam(teamId)
            .ForEvent(eventId)
            .WithTimeZone("Europe/Amsterdam")
            .WithWindow(opensAt, closesAt)
            .WithCadenceHours(72)
            .WithMinEmailIntervalHours(48));

        var specs = await QueryAsync();

        var spec = specs.ShouldHaveSingleItem();
        spec.TeamId.ShouldBe(teamId.Value);
        spec.TicketedEventId.ShouldBe(eventId.Value);
        spec.TimeZone.ShouldBe("Europe/Amsterdam");
        spec.OpensAt.ShouldBe(opensAt);
        spec.ClosesAt.ShouldBe(closesAt);
        spec.CadenceHours.ShouldBe(72);
        spec.MinEmailIntervalHours.ShouldBe(48);
    }

    [TestMethod]
    public async ValueTask HandleAsync_EventWithoutReconfirmPolicy_IsExcluded()
    {
        await SeedAsync(new EventEmailContextViewBuilder().WithoutReconfirmPolicy());

        (await QueryAsync()).ShouldBeEmpty();
    }

    [TestMethod]
    public async ValueTask HandleAsync_ArchivedEvent_IsExcluded()
    {
        await SeedAsync(new EventEmailContextViewBuilder().Archived());

        (await QueryAsync()).ShouldBeEmpty();
    }

    [TestMethod]
    public async ValueTask HandleAsync_PartialProjectionRow_IsExcluded()
    {
        // The row exists but the event-context event has not landed yet, so
        // there is no time zone to evaluate a cron in.
        await SeedAsync(new EventEmailContextViewBuilder().WithoutEventContext());

        (await QueryAsync()).ShouldBeEmpty();
    }

    [TestMethod]
    public async ValueTask HandleAsync_MixOfActiveAndInactiveEvents_ReturnsOnlyActiveOnes()
    {
        var activeEventId = TicketedEventId.New();
        await SeedAsync(new EventEmailContextViewBuilder().ForEvent(activeEventId));
        await SeedAsync(new EventEmailContextViewBuilder().WithoutReconfirmPolicy());
        await SeedAsync(new EventEmailContextViewBuilder().Archived());

        var specs = await QueryAsync();

        specs.Select(s => s.TicketedEventId).ShouldBe([activeEventId.Value]);
    }

    private static async ValueTask SeedAsync(EventEmailContextViewBuilder builder) =>
        await Environment.EmailDatabase.SeedAsync(db => db.EventEmailContexts.Add(builder.Build()));

    private async ValueTask<IReadOnlyList<ReconfirmTriggerSpecDto>> QueryAsync()
    {
        Environment.EmailDatabase.Context.ChangeTracker.Clear();
        return await new GetActiveReconfirmTriggerSpecsHandler(Environment.EmailDatabase.Context)
            .HandleAsync(new GetActiveReconfirmTriggerSpecsQuery(), testContext.CancellationToken);
    }
}
