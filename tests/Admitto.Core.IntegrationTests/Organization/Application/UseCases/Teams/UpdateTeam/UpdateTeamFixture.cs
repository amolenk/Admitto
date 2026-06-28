using Amolenk.Admitto.Testing.Builders.Organization.Application;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.Teams.UpdateTeam;

internal sealed class UpdateTeamFixture
{
    public Guid TeamId { get; private set; }
    public string OriginalName { get; } = "Acme Events";
    public uint TeamVersion { get; private set; }

    private readonly bool _archived;
    private readonly string? _replyToEmailAddress;

    private UpdateTeamFixture(bool archived = false, string? replyToEmailAddress = null)
    {
        _archived = archived;
        _replyToEmailAddress = replyToEmailAddress;
    }

    public static UpdateTeamFixture ActiveTeam(string? replyToEmailAddress = null)
        => new(replyToEmailAddress: replyToEmailAddress);

    public static UpdateTeamFixture ArchivedTeam() => new(archived: true);

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var builder = new TeamBuilder()
            .WithName(OriginalName);

        if (_archived)
        {
            builder = builder.AsArchived();
        }

        if (_replyToEmailAddress is not null)
        {
            builder = builder.WithReplyToEmailAddress(_replyToEmailAddress);
        }

        var team = builder.Build();

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Teams.Add(team);
        });

        // EF Core populates Version on the entity after SaveChangesAsync
        TeamId = team.Id.Value;
        TeamVersion = team.Version;
    }
}
