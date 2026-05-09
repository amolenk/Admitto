using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamManagement.CreateTeam;

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

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(existingTeam);
        });
    }
}
