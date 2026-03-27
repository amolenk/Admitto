using Amolenk.Admitto.Module.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Module.Organization.Domain.Tests.Entities;

[TestClass]
public sealed class UserTests
{
    [TestMethod]
    public void New_AddsUserCreatedDomainEvent()
    {
        // Act
        var sut = new UserBuilder().Build();

        // Asserts
        sut.GetDomainEvents()
            .ShouldHaveSingleItem()
            .ShouldBeAssignableTo<UserCreatedDomainEvent>()
            .EmailAddress.ShouldBe(UserBuilder.DefaultEmail);
    }

    [TestMethod]
    public void AddTeamMembership_NewTeam_AddsMembership()
    {
        // Arrange
        var teamId = TeamId.New();
        const TeamMembershipRole role = TeamMembershipRole.Organizer;
        
        var sut = new UserBuilder().Build();
    
        // Act
        sut.AddTeamMembership(teamId, role);
    
        // Assert
        sut.Memberships.ShouldHaveSingleItem().ShouldSatisfyAllConditions(m =>
        {
            m.Id.ShouldBe(teamId);
            m.Role.ShouldBe(role);
        });
    }

    [TestMethod]
    public void AddTeamMembership_MembershipAlreadyExists_ThrowsException()
    {
        // Arrange
        var teamId = TeamId.New();
        
        var sut = new UserBuilder().Build();
        sut.AddTeamMembership(teamId, TeamMembershipRole.Crew);
        
        // Act
        var result = ErrorResult.Capture(() => sut.AddTeamMembership(teamId, TeamMembershipRole.Organizer));
    
        // Assert
        result.Error.ShouldMatch(User.Errors.UserAlreadyTeamMember(sut.Id, teamId));
    }
    
    [TestMethod]
    public void AssignExternalUserId_NotYetAssigned_SetsExternalUserId()
    {
        // Arrange
        var externalUserId = ExternalUserId.New();
        
        var sut = new UserBuilder().Build();
        
        // Act
        sut.AssignExternalUserId(externalUserId);
        
        // Assert
        sut.ExternalUserId.ShouldBe(externalUserId);
    }
    
    [TestMethod]
    public void AssignExternalUserId_AlreadyAssigned_OverwritesExistingExternalUserId()
    {
        // Arrange
        var existingExternalUserId = ExternalUserId.New();
        var newExternalUserId = ExternalUserId.New();
        
        var sut = new UserBuilder().Build();
        sut.AssignExternalUserId(existingExternalUserId);
        
        // Act
        sut.AssignExternalUserId(newExternalUserId);
        
        // Assert
        sut.ExternalUserId.ShouldBe(newExternalUserId);
    }
}