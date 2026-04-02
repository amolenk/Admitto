using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamManagement.UpdateTeam;

internal sealed class UpdateTeamFixture
{
    public Guid TeamId { get; private set; }
    public string OriginalSlug { get; } = "acme";
    public string OriginalName { get; } = "Acme Events";
    public string OriginalEmail { get; } = "info@acme.org";
    public uint TeamVersion { get; private set; }

    private readonly bool _archived;

    private UpdateTeamFixture(bool archived = false)
    {
        _archived = archived;
    }

    public static UpdateTeamFixture ActiveTeam() => new();

    public static UpdateTeamFixture ArchivedTeam() => new(archived: true);

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var builder = new TeamBuilder()
            .WithSlug(OriginalSlug)
            .WithName(OriginalName)
            .WithEmail(OriginalEmail);

        if (_archived)
        {
            builder = builder.AsArchived();
        }

        var team = builder.Build();

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);
        });

        // EF Core populates Version on the entity after SaveChangesAsync
        TeamId = team.Id.Value;
        TeamVersion = team.Version;
    }
}
