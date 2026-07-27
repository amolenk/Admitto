using Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Organization.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Projections.TeamEmailContext;

[TestClass]
public sealed class TeamEmailContextProjectorTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async Task TeamDetailsUpdated_UpdatesTeamContext()
    {
        var teamId = TeamId.New();
        var projector = new TeamEmailContextProjector(Environment.EmailDatabase.Context);

        await projector.HandleAsync(
            new TeamDetailsUpdatedIntegrationEvent(
                teamId.Value, "Updated Team", "#ff0000", TeamVersion: 3),
            testContext.CancellationToken);

        await Environment.EmailDatabase.AssertAsync(async db =>
        {
            var view = await db.TeamEmailContexts.SingleAsync(
                c => c.TeamId == teamId,
                testContext.CancellationToken);
            view.TeamName.ShouldBe("Updated Team");
            view.AccentColor.ShouldBe(AccentColor.From("#ff0000"));
            view.TeamVersion.ShouldBe(3u);
        });
    }

    [TestMethod]
    public async Task TeamDetailsUpdated_LateOlderVersion_DoesNotOverwriteTeamContext()
    {
        var teamId = TeamId.New();
        var projector = new TeamEmailContextProjector(Environment.EmailDatabase.Context);

        await projector.HandleAsync(
            new TeamDetailsUpdatedIntegrationEvent(teamId.Value, "Blue Team", "#0000ff", TeamVersion: 3),
            testContext.CancellationToken);
        await projector.HandleAsync(
            new TeamDetailsUpdatedIntegrationEvent(teamId.Value, "Green Team", "#00ff00", TeamVersion: 2),
            testContext.CancellationToken);

        await Environment.EmailDatabase.AssertAsync(async db =>
        {
            var view = await db.TeamEmailContexts.SingleAsync(
                c => c.TeamId == teamId,
                testContext.CancellationToken);
            view.TeamName.ShouldBe("Blue Team");
            view.AccentColor.ShouldBe(AccentColor.From("#0000ff"));
            view.TeamVersion.ShouldBe(3u);
        });
    }
}
