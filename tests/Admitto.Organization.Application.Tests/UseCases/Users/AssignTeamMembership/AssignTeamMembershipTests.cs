using Amolenk.Admitto.Organization.Application.Tests.Infrastructure;
using Amolenk.Admitto.Organization.Application.UseCases.Users.AssignTeamMembership;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Organization.Application.Tests.UseCases.Users.AssignTeamMembership;

[TestClass]
public sealed class AssignTeamMembershipTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask AssignTeamMembership_UserDoesNotExist_CreatesUserAndAssignsMembership()
    {
        // Arrange
        var teamId = TeamId.New();
        var command = NewAssignTeamMembershipCommand(teamId);
        var sut = NewAssignTeamMembershipHandler();

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            // Verify that a user with one membership has been created.
            var user = await dbContext.Users.SingleOrDefaultAsync(testContext.CancellationToken);

            user.ShouldNotBeNull();
            user.ExternalUserId.ShouldBeNull();
            user.EmailAddress.ShouldBe(command.EmailAddress);
            user.Memberships.ShouldHaveSingleItem().ShouldSatisfyAllConditions(m =>
            {
                m.Id.ShouldBe(teamId);
                m.Role.ShouldBe(command.Role);
            });
        });
    }
    
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
        await Environment.Database.AssertAsync(async dbContext =>
        {
            // Verify that the membership is added to the existing user.
            var user = await dbContext.Users.FindAsync([fixture.UserId], testContext.CancellationToken);

            user.ShouldNotBeNull();
            user.ExternalUserId.ShouldBeNull();
            user.EmailAddress.ShouldBe(command.EmailAddress);
            user.Memberships.ShouldHaveSingleItem().ShouldSatisfyAllConditions(m =>
            {
                m.Id.ShouldBe(fixture.TeamId);
                m.Role.ShouldBe(command.Role);
            });
        });
    }

    private static AssignTeamMembershipCommand NewAssignTeamMembershipCommand(
        TeamId teamId,
        EmailAddress? emailAddress = null,
        TeamMembershipRole? role = null)
    {
        emailAddress ??= EmailAddress.From("alice@example.com");
        role ??= TeamMembershipRole.Crew;

        return new AssignTeamMembershipCommand(teamId, emailAddress.Value, role.Value);
    }
    
    private static AssignTeamMembershipHandler NewAssignTeamMembershipHandler() =>
        new (Environment.Database.Context);
}