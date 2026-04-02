using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamMembershipManagement.ChangeTeamMembershipRole;

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
        var teamId = Amolenk.Admitto.Module.Shared.Kernel.ValueObjects.TeamId.From(TeamId);
        var user = new UserBuilder()
            .WithEmailAddress(global::Amolenk.Admitto.Module.Shared.Kernel.ValueObjects.EmailAddress.From(EmailAddress))
            .WithMembership(teamId, TeamMembershipRole.Crew)
            .Build();

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });

        UserId = user.Id.Value;
    }
}
