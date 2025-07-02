using Amolenk.Admitto.Application.UseCases.Auth.AssignTeamRole;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Amolenk.Admitto.IntegrationTests.TestHelpers.Builders;

namespace Amolenk.Admitto.IntegrationTests.UseCases.Auth;

[TestClass]
public class AssignTeamRoleTests : FullStackTestsBase
{
    private Team _testTeam = null!;
        
    [TestInitialize]
    public override async Task TestInitialize()
    {
        await base.TestInitialize();

        await SeedDatabaseAsync(context =>
        {
            _testTeam = new TeamBuilder().Build();
            context.Teams.Add(_testTeam);
        });
    }
    
    [TestMethod]
    public async ValueTask UserDoesNotHaveRole_AddsRole()
    {
        // Arrange
        
        // Ensure user exists
        const string email = "bob@example.com";
        var user = await Identity.IdentityService.AddUserAsync(email);
        
        var command = new AssignTeamRoleCommand(user.Id, _testTeam.Id, TeamMemberRole.Organizer);

        // Act
        await HandleCommand<AssignTeamRoleCommand, AssignTeamRoleHandler>(command);
        
        // Assert
        var userRoles = (await Authorization.AuthorizationService.GetTeamRolesAsync(
            user.Id, _testTeam.Id)).ToList();
        
        userRoles.Count.ShouldBe(1);
        userRoles[0].ShouldBe<TeamMemberRole>(TeamMemberRole.Organizer);
    }
    
    [TestMethod]
    public async ValueTask UserAlreadyHasRole_DoesNotAddDuplicateRole()
    {
        // Arrange
        
        // Ensure user exists
        const string email = "bob@example.com";
        var user = await Identity.IdentityService.AddUserAsync(email);
        
        // Ensure the user already has the role
        await Authorization.AuthorizationService.AddTeamRoleAsync(user.Id, _testTeam.Id, 
            TeamMemberRole.Organizer);

        var command = new AssignTeamRoleCommand(user.Id, _testTeam.Id, TeamMemberRole.Organizer);

        // Act
        await HandleCommand<AssignTeamRoleCommand, AssignTeamRoleHandler>(command);
        
        // Assert
        var userRoles = (await Authorization.AuthorizationService.GetTeamRolesAsync(
            user.Id, _testTeam.Id)).ToList();
        
        userRoles.Count.ShouldBe(1);
        userRoles[0].ShouldBe<TeamMemberRole>(TeamMemberRole.Organizer);
    }
}
