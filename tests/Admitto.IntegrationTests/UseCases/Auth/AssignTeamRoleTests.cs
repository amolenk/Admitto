using Amolenk.Admitto.Application.UseCases.Auth.AssignTeamRole;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.IntegrationTests.UseCases.Auth;

[TestClass]
public class AssignTeamRoleTests : FullStackTestsBase
{
    [TestMethod]
    public async ValueTask UserDoesNotHaveRole_AddsRole()
    {
        // Arrange
        
        // Ensure user exists
        const string email = "bob@example.com";
        var user = await Identity.IdentityService.AddUserAsync(email);
        
        var command = new AssignTeamRoleCommand(user.Id, DefaultTeam.Id, TeamMemberRole.Organizer);

        // Act
        await HandleCommand<AssignTeamRoleCommand, AssignTeamRoleHandler>(command);
        
        // Assert
        var userRoles = (await Authorization.AuthorizationService.GetTeamRolesAsync(
            user.Id, DefaultTeam.Id)).ToList();
        
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
        await Authorization.AuthorizationService.AddTeamRoleAsync(user.Id, DefaultTeam.Id, 
            TeamMemberRole.Organizer);

        var command = new AssignTeamRoleCommand(user.Id, DefaultTeam.Id, TeamMemberRole.Organizer);

        // Act
        await HandleCommand<AssignTeamRoleCommand, AssignTeamRoleHandler>(command);
        
        // Assert
        var userRoles = (await Authorization.AuthorizationService.GetTeamRolesAsync(
            user.Id, DefaultTeam.Id)).ToList();
        
        userRoles.Count.ShouldBe(1);
        userRoles[0].ShouldBe<TeamMemberRole>(TeamMemberRole.Organizer);
    }
}
