using Amolenk.Admitto.Module.Organization.Application.Mapping;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.AssignTeamMembership;
using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Organization.Tests.Application.Builders;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamMembershipManagement.AssignTeamMembership;

[TestClass]
public sealed class AssignTeamMembershipTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC001_AssignTeamMembership_UserDoesNotExist_CreatesUserAndAssignsMembership()
    {
        // Arrange
        var fixture = AssignTeamMembershipFixture.TeamOnly();
        await fixture.SetupAsync(Environment);

        var command = NewAssignTeamMembershipCommand(fixture.TeamId);
        var sut = NewAssignTeamMembershipHandler();

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(testContext.CancellationToken);

            user.ShouldNotBeNull();
            user.ExternalUserId.ShouldBeNull();
            user.EmailAddress.Value.ShouldBe(command.EmailAddress);
            user.Memberships.ShouldHaveSingleItem().ShouldSatisfyAllConditions(m =>
            {
                m.Id.Value.ShouldBe(fixture.TeamId);
                m.Role.ToDto().ShouldBe(command.Role);
            });
        });
    }

    [TestMethod]
    public async ValueTask SC002_AssignTeamMembership_UserAlreadyExists_AssignsMembership()
    {
        // Arrange
        var fixture = AssignTeamMembershipFixture.UserExists();
        await fixture.SetupAsync(Environment);

        var command = NewAssignTeamMembershipCommand(fixture.TeamId, fixture.EmailAddress);
        var sut = NewAssignTeamMembershipHandler();

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var user = await dbContext.Users.FindAsync([UserId.From(fixture.UserId)], testContext.CancellationToken);

            user.ShouldNotBeNull();
            user.ExternalUserId.ShouldBeNull();
            user.EmailAddress.Value.ShouldBe(command.EmailAddress);
            user.Memberships.ShouldHaveSingleItem().ShouldSatisfyAllConditions(m =>
            {
                m.Id.Value.ShouldBe(fixture.TeamId);
                m.Role.ToDto().ShouldBe(command.Role);
            });
        });
    }

    [TestMethod]
    public async ValueTask SC003_AssignTeamMembership_UserAlreadyMember_ThrowsAlreadyMember()
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

        await Environment.Database.SeedAsync(dbContext => dbContext.Users.Add(user));

        var duplicateCommand = NewAssignTeamMembershipCommand(fixture.TeamId, email);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await NewAssignTeamMembershipHandler().HandleAsync(duplicateCommand, testContext.CancellationToken));

        exception.Error.Code.ShouldBe("user.already_team_member");
    }

    [TestMethod]
    public async ValueTask SC012_AssignTeamMembership_UserHadPendingDeprovisioning_CancelsDeprovisioning()
    {
        // Arrange
        var fixture = AssignTeamMembershipFixture.TeamOnly();
        await fixture.SetupAsync(Environment);

        var teamIdVo = TeamId.From(fixture.TeamId);

        var user = new UserBuilder()
            .WithEmailAddress(EmailAddress.From("alice@example.com"))
            .WithMembership(teamIdVo, TeamMembershipRole.Crew)
            .Build();

        await Environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });

        // Remove the membership to trigger DeprovisionAfter
        await Environment.Database.WithContextAsync(async dbContext =>
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
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var reloadedUser = await dbContext.Users.FindAsync(
                [UserId.From(user.Id.Value)], testContext.CancellationToken);

            reloadedUser.ShouldNotBeNull();
            reloadedUser.DeprovisionAfter.ShouldBeNull();
        });
    }

    [TestMethod]
    public async ValueTask SC010_AssignTeamMembership_NewUser_RaisesUserCreatedDomainEvent()
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
        var trackedUser = Environment.Database.Context.ChangeTracker.Entries<User>()
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
        new(Environment.Database.Context);
}