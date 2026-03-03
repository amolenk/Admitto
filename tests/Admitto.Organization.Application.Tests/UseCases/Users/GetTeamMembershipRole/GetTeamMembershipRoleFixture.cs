using Amolenk.Admitto.Organization.Application.Mapping;
using Amolenk.Admitto.Organization.Application.Tests.Infrastructure.Hosting;
using Amolenk.Admitto.Organization.Contracts;
using Amolenk.Admitto.Organization.Domain.Tests.Builders;

namespace Amolenk.Admitto.Organization.Application.Tests.UseCases.Users.GetTeamMembershipRole;

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

        if (Role is not null)
        {
            user.AddTeamMembership(
                Shared.Kernel.ValueObjects.TeamId.From(TeamId),
                Role.Value.ToDomain());
        }
        
        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });
        
        UserId = user.Id.Value;
    }
}