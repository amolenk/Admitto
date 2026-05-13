using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Domain;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamMembershipManagement.ChangeTeamMembershipRole;

internal sealed class ChangeTeamMembershipRoleFixture
{
    public Guid TeamId { get; } = Guid.NewGuid();
    public string EmailAddress { get; } = "alice@example.com";
    public Guid UserId { get; private set; }

    private ChangeTeamMembershipRoleFixture()
    {
    }

    public static ChangeTeamMembershipRoleFixture MemberExists() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var teamId = Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.TeamId.From(TeamId);
        var user = new UserBuilder()
            .WithEmailAddress(global::Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.EmailAddress.From(EmailAddress))
            .WithMembership(teamId, TeamMembershipRole.Crew)
            .Build();

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });

        UserId = user.Id.Value;
    }
}
