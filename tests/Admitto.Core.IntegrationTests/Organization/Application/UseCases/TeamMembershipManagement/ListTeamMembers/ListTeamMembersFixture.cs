using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Domain;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamMembershipManagement.ListTeamMembers;

internal sealed class ListTeamMembersFixture
{
    public Guid TeamId { get; } = Guid.NewGuid();

    private readonly bool _withMembers;

    private ListTeamMembersFixture(bool withMembers)
    {
        _withMembers = withMembers;
    }

    public static ListTeamMembersFixture TeamWithMembers() => new(withMembers: true);

    public static ListTeamMembersFixture EmptyTeam() => new(withMembers: false);

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (!_withMembers) return;

        var teamId = Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId.From(TeamId);
        var alice = new UserBuilder()
            .WithEmailAddress(EmailAddress.From("alice@example.com"))
            .WithMembership(teamId, TeamMembershipRole.Owner)
            .Build();
        var bob = new UserBuilder()
            .WithEmailAddress(EmailAddress.From("bob@example.com"))
            .WithMembership(teamId, TeamMembershipRole.Crew)
            .Build();

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(alice);
            dbContext.Users.Add(bob);
        });
    }
}
