using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamManagement.GetTeamId;

internal sealed class GetTeamIdFixture
{
    public Guid TeamId { get; private set; }
    public string TeamSlug { get; } = "acme";

    private GetTeamIdFixture()
    {
    }

    public static GetTeamIdFixture TeamExists() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .WithSlug(TeamSlug)
            .Build();

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);
        });

        TeamId = team.Id.Value;
    }
}
