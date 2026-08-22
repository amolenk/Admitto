using Amolenk.Admitto.Core.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Testing.Builders.Organization.Domain;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Core.Organization.Domain.Tests.Entities;

[TestClass]
public sealed class UserTests
{
    // When a new user is created
    // Then a UserCreated domain event is raised with the user's email address
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

    // Given a user with no membership in a team
    // When a membership is added for that team with a role
    // Then the membership is added with the given team and role
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
            m.TeamId.ShouldBe(teamId);
            m.Role.ShouldBe(role);
        });
    }

    // Given a user who is already a member of a team
    // When a membership is added again for the same team
    // Then it throws UserAlreadyTeamMember
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

    // Given a user whose last membership was removed and who has a pending deprovisioning deadline
    // When a new team membership is added
    // Then the pending deprovisioning is cancelled
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

    // Given a user with an existing membership in a team
    // When the membership role is changed
    // Then the membership's role is updated
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

    // Given a user with no membership in a team
    // When the membership role for that team is changed
    // Then it throws UserNotTeamMember
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

    // Given a user with a single team membership
    // When that membership is removed
    // Then a future deprovisioning deadline is set
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

    // Given a user with memberships in two teams
    // When the membership in one team is removed
    // Then no deprovisioning deadline is set
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

    // Given a user with no membership in a team
    // When that team's membership is removed
    // Then it throws UserNotTeamMember
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

    // Given a user with a pending deprovisioning deadline
    // When deprovisioning is cancelled
    // Then the deprovisioning deadline is cleared
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
    
    // Given a user with no external user id assigned
    // When an external user id is assigned
    // Then the user's external user id is set
    [TestMethod]
    public void AssignExternalUserId_NotYetAssigned_SetsExternalUserId()
    {
        // Arrange
        var externalUserId = ExternalUserId.From("external-user-1");
        
        var sut = new UserBuilder().Build();
        
        // Act
        sut.AssignExternalUserId(externalUserId);
        
        // Assert
        sut.ExternalUserId.ShouldBe(externalUserId);
    }
    
    // Given a user who already has an external user id assigned
    // When a different external user id is assigned
    // Then the existing external user id is overwritten with the new one
    [TestMethod]
    public void AssignExternalUserId_AlreadyAssigned_OverwritesExistingExternalUserId()
    {
        // Arrange
        var existingExternalUserId = ExternalUserId.From("external-user-1");
        var newExternalUserId = ExternalUserId.From("external-user-2");
        
        var sut = new UserBuilder().Build();
        sut.AssignExternalUserId(existingExternalUserId);
        
        // Act
        sut.AssignExternalUserId(newExternalUserId);
        
        // Assert
        sut.ExternalUserId.ShouldBe(newExternalUserId);
    }
}