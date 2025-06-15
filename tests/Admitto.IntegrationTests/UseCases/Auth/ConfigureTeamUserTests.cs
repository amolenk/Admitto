using Amolenk.Admitto.Application.UseCases.Auth.AssignTeamRole;
using Amolenk.Admitto.Application.UseCases.Auth.ConfigureTeamUser;
using Amolenk.Admitto.Application.UseCases.Auth.ConfigureTeamUser.EventHandlers;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using UserDataFactory = Amolenk.Admitto.IntegrationTests.TestHelpers.Data.UserDataFactory;

namespace Amolenk.Admitto.IntegrationTests.UseCases.Auth;

[TestClass]
public class ConfigureTeamUserTests : BaseForWorkerTests
{
    [TestMethod]
    public async ValueTask TeamMemberAddedDomainEvent_ConfiguresTeamUser()
    {
        // Arrange
        var eventHandler = new TeamMemberAddedDomainEventHandler(
            WorkerHostServiceProvider.GetRequiredService<ConfigureTeamUserHandler>());

        var teamMember = UserDataFactory.CreateTeamMember();
        var domainEvent = new TeamMemberAddedDomainEvent(DefaultTeam.Id, teamMember);

        // Act
        await eventHandler.HandleAsync(domainEvent, CancellationToken.None);
        
        // Assert
        var createdUser = await Identity.IdentityService.GetUserByEmailAsync(domainEvent.Member.Email);
        createdUser.ShouldNotBeNull();
    }
    
    [TestMethod]
    public async ValueTask UserDoesNotExist_AddsUser()
    {
        // Arrange
        var commandHandler = WorkerHostServiceProvider.GetRequiredService<ConfigureTeamUserHandler>();     
        
        var teamMember = UserDataFactory.CreateTeamMember();
        var command = new ConfigureTeamUserCommand(DefaultTeam.Id, teamMember.Email, teamMember.Role);

        // Act
        await commandHandler.HandleAsync(command, CancellationToken.None);
        
        // Assert
        var createdUser = await Identity.IdentityService.GetUserByEmailAsync(command.Email);
        createdUser.ShouldNotBeNull();
        
        (await Identity.IdentityService.GetUsersAsync()).Count().ShouldBe(2); 
    }
    
    [TestMethod]
    public async ValueTask UserAlreadyExists_DoesNotAddDuplicateUser()
    {
        // Arrange
        var commandHandler = WorkerHostServiceProvider.GetRequiredService<ConfigureTeamUserHandler>();     
        
        var teamMember = TeamMember.Create(UserDataFactory.TestUserEmail, TeamMemberRole.Manager);
        var command = new ConfigureTeamUserCommand(DefaultTeam.Id, teamMember.Email, teamMember.Role);

        // Act
        await commandHandler.HandleAsync(command, CancellationToken.None);
        
        // Assert
        (await Identity.IdentityService.GetUsersAsync()).Count().ShouldBe(1); 
    }
    
    [TestMethod]
    public async ValueTask SendsAssignTeamRoleCommand()
    {
        // Arrange
        var commandHandler = WorkerHostServiceProvider.GetRequiredService<ConfigureTeamUserHandler>();     
        
        var teamMember = TeamMember.Create(UserDataFactory.TestUserEmail, TeamMemberRole.Manager);
        var command = new ConfigureTeamUserCommand(DefaultTeam.Id, teamMember.Email, teamMember.Role);

        // Act
        await commandHandler.HandleAsync(command, CancellationToken.None);
        
        // Assert
        await UnitOfWork.SaveChangesAsync();
        await QueueStorage.MessageQueue.ShouldContainMessageAsync<AssignTeamRoleCommand>(message =>
        {
            message.UserId.ShouldBe(UserDataFactory.TestUserId);
            message.TeamId.ShouldBe(command.TeamId);
            message.Role.ShouldBe(command.Role);
        });
    }
}
