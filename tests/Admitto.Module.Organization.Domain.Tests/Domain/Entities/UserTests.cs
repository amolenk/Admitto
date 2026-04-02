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
    public void AddTeamMembership_PendingDeprovisioning_CancelsDeprovisioning()
    {
        // Arrange — simulate a user whose last membership was removed (has a deprovisioning deadline)
        var teamId = TeamId.New();

        var sut = new UserBuilder()
            .WithMembership(teamId)
            .Build();

        sut.RemoveTeamMembership(teamId);
        sut.DeprovisionAfter.ShouldNotBeNull();

        // Act — re-add the membership
        var secondTeamId = TeamId.New();
        sut.AddTeamMembership(secondTeamId, TeamMembershipRole.Crew);

        // Assert
        sut.DeprovisionAfter.ShouldBeNull();
    }

    [TestMethod]
    public void ChangeTeamMembershipRole_ExistingMembership_UpdatesRole()
    {
        // Arrange
        var teamId = TeamId.New();

        var sut = new UserBuilder()
            .WithMembership(teamId, TeamMembershipRole.Crew)
            .Build();

        // Act
        sut.ChangeTeamMembershipRole(teamId, TeamMembershipRole.Owner);

        // Assert
        sut.Memberships.ShouldHaveSingleItem().Role.ShouldBe(TeamMembershipRole.Owner);
    }

    [TestMethod]
    public void ChangeTeamMembershipRole_UserNotMember_ThrowsException()
    {
        // Arrange
        var sut = new UserBuilder().Build();
        var teamId = TeamId.New();

        // Act
        var result = ErrorResult.Capture(() => sut.ChangeTeamMembershipRole(teamId, TeamMembershipRole.Owner));

        // Assert
        result.Error.ShouldMatch(User.Errors.UserNotTeamMember(sut.Id, teamId));
    }

    [TestMethod]
    public void RemoveTeamMembership_LastMembership_SetsDeprovisionAfter()
    {
        // Arrange
        var teamId = TeamId.New();

        var sut = new UserBuilder()
            .WithMembership(teamId, TeamMembershipRole.Crew)
            .Build();

        // Act
        sut.RemoveTeamMembership(teamId);

        // Assert
        sut.DeprovisionAfter.ShouldNotBeNull();
        sut.DeprovisionAfter.Value.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [TestMethod]
    public void RemoveTeamMembership_NotLastMembership_DoesNotSetDeprovisionAfter()
    {
        // Arrange
        var teamId1 = TeamId.New();
        var teamId2 = TeamId.New();

        var sut = new UserBuilder()
            .WithMembership(teamId1, TeamMembershipRole.Crew)
            .WithMembership(teamId2, TeamMembershipRole.Owner)
            .Build();

        // Act
        sut.RemoveTeamMembership(teamId1);

        // Assert
        sut.DeprovisionAfter.ShouldBeNull();
    }

    [TestMethod]
    public void RemoveTeamMembership_UserNotMember_ThrowsException()
    {
        // Arrange
        var sut = new UserBuilder().Build();
        var teamId = TeamId.New();

        // Act
        var result = ErrorResult.Capture(() => sut.RemoveTeamMembership(teamId));

        // Assert
        result.Error.ShouldMatch(User.Errors.UserNotTeamMember(sut.Id, teamId));
    }

    [TestMethod]
    public void CancelDeprovisioning_WithPendingDeprovisioning_ClearsDeprovisionAfter()
    {
        // Arrange
        var teamId = TeamId.New();

        var sut = new UserBuilder()
            .WithMembership(teamId)
            .Build();

        sut.RemoveTeamMembership(teamId);
        sut.DeprovisionAfter.ShouldNotBeNull();

        // Act
        sut.CancelDeprovisioning();

        // Assert
        sut.DeprovisionAfter.ShouldBeNull();
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