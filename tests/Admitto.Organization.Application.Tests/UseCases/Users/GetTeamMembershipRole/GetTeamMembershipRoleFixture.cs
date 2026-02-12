using Amolenk.Admitto.Organization.Application.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Organization.Domain.Tests.Builders;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.Tests.UseCases.Users.GetTeamMembershipRole;

internal sealed class GetTeamMembershipRoleFixture
{
    public TeamId TeamId { get; } = TeamId.New();
    public UserId UserId { get; private set; }
    public TeamMembershipRole? Role { get; private set; } = TeamMembershipRole.Organizer;

    private GetTeamMembershipRoleFixture()
    {
    }

    public static GetTeamMembershipRoleFixture HappyFlow() => new();

    public static GetTeamMembershipRoleFixture UserWithoutTeamMembership() => new()
    {
        Role = null
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var user = new UserBuilder().Build();

        if (Role is not null)
        {
            user.AddTeamMembership(TeamId, Role.Value);
        }
        
        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });
        
        UserId = user.Id;
    }
}