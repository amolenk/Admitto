using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.Users.GetTeamMembershipRole;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.Users.GetTeamMembershipRole;

[TestClass]
public sealed class GetTeamMembershipRoleTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask GetTeamMembershipRole_UserExistsWithTeamMembership_ReturnsRole()
    {
        // Arrange
        var fixture = GetTeamMembershipRoleFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var command = NewGetTeamMembershipRoleQuery(fixture.TeamId, fixture.UserId);
        var sut = NewGetTeamMembershipRoleHandler();

        // Act
        var role = await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        role.ShouldBe(fixture.Role);
    }
    
    [TestMethod]
    public async ValueTask GetTeamMembershipRole_UserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = NewGetTeamMembershipRoleQuery(teamId, userId);
        var sut = NewGetTeamMembershipRoleHandler();

        // Act
        var role = await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        role.ShouldBeNull();    
    }
    
    [TestMethod]
    public async ValueTask GetTeamMembershipRole_UserWithoutTeamMembership_ReturnsNull()
    {
        // Arrange
        var fixture = GetTeamMembershipRoleFixture.UserWithoutTeamMembership();
        await fixture.SetupAsync(Environment);

        var command = NewGetTeamMembershipRoleQuery(fixture.TeamId, fixture.UserId);
        var sut = NewGetTeamMembershipRoleHandler();

        // Act
        var role = await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        role.ShouldBeNull();    
    }
    
    private static GetTeamMembershipRoleQuery NewGetTeamMembershipRoleQuery(Guid teamId, Guid userId) =>
        new (teamId, userId);
    
    private static GetTeamMembershipRoleHandler NewGetTeamMembershipRoleHandler() =>
        new (Environment.Database.Context);
}