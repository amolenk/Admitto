using Amolenk.Admitto.Core.Organization.Application.Mapping;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.AssignTeamMembership;
using Amolenk.Admitto.Core.Organization.Contracts;
using Amolenk.Admitto.Core.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Domain;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamMemberships.AssignTeamMembership;

[TestClass]
public sealed class AssignTeamMembershipTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a team with no user for the given email address
    // When a team membership is assigned to that email address
    // Then a new user is created and assigned the membership with the given role
    [TestMethod]
    public async ValueTask AssignTeamMembership_UserDoesNotExist_CreatesUserAndAssignsMembership()
    {
        // Arrange
        var fixture = AssignTeamMembershipFixture.TeamOnly();
        await fixture.SetupAsync(Environment);

        var command = NewAssignTeamMembershipCommand(fixture.TeamId);
        var sut = NewAssignTeamMembershipHandler();

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.OrganizationDatabase.AssertAsync(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(testContext.CancellationToken);

            user.ShouldNotBeNull();
            user.ExternalUserId.ShouldBeNull();
            user.EmailAddress.Value.ShouldBe(command.EmailAddress);
            user.Memberships.ShouldHaveSingleItem().ShouldSatisfyAllConditions(m =>
            {
                m.TeamId.Value.ShouldBe(fixture.TeamId);
                m.Role.ToDto().ShouldBe(command.Role);
            });
        });
    }

    // Given a user already exists but is not a member of the team
    // When a team membership is assigned to that user's email address
    // Then the existing user is assigned the membership with the given role
    [TestMethod]
    public async ValueTask AssignTeamMembership_UserAlreadyExists_AssignsMembership()
    {
        // Arrange
        var fixture = AssignTeamMembershipFixture.UserExists();
        await fixture.SetupAsync(Environment);

        var command = NewAssignTeamMembershipCommand(fixture.TeamId, fixture.EmailAddress);
        var sut = NewAssignTeamMembershipHandler();

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.OrganizationDatabase.AssertAsync(async dbContext =>
        {
            var user = await dbContext.Users.FindAsync([UserId.From(fixture.UserId)], testContext.CancellationToken);

            user.ShouldNotBeNull();
            user.ExternalUserId.ShouldBeNull();
            user.EmailAddress.Value.ShouldBe(command.EmailAddress);
            user.Memberships.ShouldHaveSingleItem().ShouldSatisfyAllConditions(m =>
            {
                m.TeamId.Value.ShouldBe(fixture.TeamId);
                m.Role.ToDto().ShouldBe(command.Role);
            });
        });
    }

    // Given a user is already a member of the team
    // When a team membership is assigned to that user's email address again
    // Then it throws an "already team member" error
    [TestMethod]
    public async ValueTask AssignTeamMembership_UserAlreadyMember_ThrowsAlreadyMember()
    {
        // Arrange
        var fixture = AssignTeamMembershipFixture.TeamOnly();
        await fixture.SetupAsync(Environment);

        const string email = "alice@example.com";
        var teamId = TeamId.From(fixture.TeamId);

        var user = new UserBuilder()
            .WithEmailAddress(EmailAddress.From(email))
            .WithMembership(teamId, TeamMembershipRole.Crew)
            .Build();

        await Environment.OrganizationDatabase.SeedAsync(dbContext => dbContext.Users.Add(user));

        var duplicateCommand = NewAssignTeamMembershipCommand(fixture.TeamId, email);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await NewAssignTeamMembershipHandler().HandleAsync(duplicateCommand, testContext.CancellationToken));

        exception.Error.ShouldMatch(User.Errors.UserAlreadyTeamMember(user.Id, teamId));
    }

    // Given a user was removed from the team and is pending deprovisioning
    // When the user is re-assigned a membership on the same team
    // Then the pending deprovisioning is cancelled
    [TestMethod]
    public async ValueTask AssignTeamMembership_UserHadPendingDeprovisioning_CancelsDeprovisioning()
    {
        // Arrange
        var fixture = AssignTeamMembershipFixture.TeamOnly();
        await fixture.SetupAsync(Environment);

        var teamIdVo = TeamId.From(fixture.TeamId);

        var user = new UserBuilder()
            .WithEmailAddress(EmailAddress.From("alice@example.com"))
            .WithMembership(teamIdVo, TeamMembershipRole.Crew)
            .Build();

        await Environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });

        // Remove the membership to trigger DeprovisionAfter
        await Environment.OrganizationDatabase.WithContextAsync(async dbContext =>
        {
            var tracked = await dbContext.Users.FindAsync([UserId.From(user.Id.Value)], testContext.CancellationToken);
            tracked!.RemoveTeamMembership(teamIdVo);
            await dbContext.SaveChangesAsync(testContext.CancellationToken);
            dbContext.ChangeTracker.Clear();
        });

        // Now re-add to the same team
        var reassignCommand = NewAssignTeamMembershipCommand(fixture.TeamId, "alice@example.com");
        var sut = NewAssignTeamMembershipHandler();

        // Act
        await sut.HandleAsync(reassignCommand, testContext.CancellationToken);

        // Assert
        await Environment.OrganizationDatabase.AssertAsync(async dbContext =>
        {
            var reloadedUser = await dbContext.Users.FindAsync(
                [UserId.From(user.Id.Value)], testContext.CancellationToken);

            reloadedUser.ShouldNotBeNull();
            reloadedUser.DeprovisionAfter.ShouldBeNull();
        });
    }

    // Given a team with no user for the given email address
    // When a team membership is assigned to that email address, creating a new user
    // Then a UserCreated domain event is queued on the new user entity
    [TestMethod]
    public async ValueTask AssignTeamMembership_NewUser_RaisesUserCreatedDomainEvent()
    {
        // Arrange
        // Note: the provisioning chain (UserCreatedDomainEvent → RegisterExternalUser)
        // executes via DomainEventsInterceptor which is not active in integration tests.
        // This test verifies the domain event is queued on the entity before SaveChanges.
        var fixture = AssignTeamMembershipFixture.TeamOnly();
        await fixture.SetupAsync(Environment);

        const string email = "alice@example.com";
        var command = new AssignTeamMembershipCommand(fixture.TeamId, email, TeamMembershipRoleDto.Crew);
        var sut = NewAssignTeamMembershipHandler();

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert — check the newly tracked user entity has a UserCreatedDomainEvent queued
        var trackedUser = Environment.OrganizationDatabase.Context.ChangeTracker.Entries<User>()
            .Select(e => e.Entity)
            .SingleOrDefault(u => u.EmailAddress.Value == email);

        trackedUser.ShouldNotBeNull();
        trackedUser.GetDomainEvents()
            .OfType<UserCreatedDomainEvent>()
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(e =>
            {
                e.UserId.ShouldBe(trackedUser.Id);
                e.EmailAddress.Value.ShouldBe(email);
            });
    }

    private static AssignTeamMembershipCommand NewAssignTeamMembershipCommand(
        Guid teamId,
        string? emailAddress = null,
        TeamMembershipRoleDto? role = null)
    {
        emailAddress ??= "alice@example.com";
        role ??= TeamMembershipRoleDto.Crew;

        return new AssignTeamMembershipCommand(teamId, emailAddress, role.Value);
    }

    private static AssignTeamMembershipHandler NewAssignTeamMembershipHandler() =>
        new(Environment.OrganizationDatabase.Context);
}
