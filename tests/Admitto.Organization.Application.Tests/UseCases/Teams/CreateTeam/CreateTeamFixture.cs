using Amolenk.Admitto.Organization.Application.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Organization.Domain.Entities;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.Tests.UseCases.Teams.CreateTeam;

internal sealed class CreateTeamFixture
{
    private bool _seedExistingTeamWithSameSlug;

    public string TeamSlug { get; } = "team-alpha";
    public string ExistingTeamName { get; } = "Existing Team";
    public string ExistingTeamEmailAddress { get; } = "existing@example.com";

    private CreateTeamFixture()
    {
    }

    public static CreateTeamFixture DuplicateSlug() => new()
    {
        _seedExistingTeamWithSameSlug = true
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (!_seedExistingTeamWithSameSlug)
        {
            return;
        }

        var existingTeam = Team.Create(
            Slug.From(TeamSlug),
            DisplayName.From(ExistingTeamName),
            EmailAddress.From(ExistingTeamEmailAddress));

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(existingTeam);
        });
    }
}
