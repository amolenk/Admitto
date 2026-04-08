using Amolenk.Admitto.Module.Organization.Application.Mapping;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Organization.Domain.Tests.Builders;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.Users.GetTeamMembershipRole;

internal sealed class GetTeamMembershipRoleFixture
{
    public Guid TeamId { get; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public TeamMembershipRoleDto? Role { get; private set; } = TeamMembershipRoleDto.Organizer;

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
        var externalUserId = Module.Organization.Domain.ValueObjects.ExternalUserId.New();
        user.AssignExternalUserId(externalUserId);

        if (Role is not null)
        {
            user.AddTeamMembership(
                Module.Shared.Kernel.ValueObjects.TeamId.From(TeamId),
                Role.Value.ToDomain());
        }
        
        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });
        
        UserId = externalUserId.Value;
    }
}