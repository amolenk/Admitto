using Amolenk.Admitto.Testing.Builders.Organization.Application;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamManagement.GetTeam;

internal sealed class GetTeamFixture
{
    public Guid TeamId { get; private set; }
    public string TeamSlug { get; } = "acme";
    public string TeamName { get; } = "Acme Events";
    public string TeamEmail { get; } = "info@acme.org";

    private GetTeamFixture()
    {
    }

    public static GetTeamFixture TeamExists() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var team = new TeamBuilder()
            .WithName(TeamName)
            .WithEmail(TeamEmail)
            .Build();

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);
        });

        TeamId = team.Id.Value;
    }
}
