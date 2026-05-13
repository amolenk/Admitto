using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamManagement.CreateTeam;

internal sealed class CreateTeamFixture
{
    public string ExistingTeamName { get; } = "Existing Team";
    public string ExistingTeamEmailAddress { get; } = "existing@example.com";

    private CreateTeamFixture()
    {
    }

    public static CreateTeamFixture WithExistingTeam() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var existingTeam = Team.Create(
            DisplayName.From(ExistingTeamName),
            EmailAddress.From(ExistingTeamEmailAddress));

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(existingTeam);
        });
    }
}
