using Amolenk.Admitto.Organization.Application.Mapping;
using Amolenk.Admitto.Organization.Application.Tests.Infrastructure;
using Amolenk.Admitto.Organization.Application.UseCases.Users.AssignTeamMembership;
using Amolenk.Admitto.Organization.Contracts;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Organization.Application.Tests.UseCases.Users.AssignTeamMembership;

[TestClass]
public sealed class AssignTeamMembershipTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask AssignTeamMembership_UserDoesNotExist_CreatesUserAndAssignsMembership()
    {
        // Arrange
        var teamId = Guid.NewGuid();
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
            user.EmailAddress.Value.ShouldBe(command.EmailAddress);
            user.Memberships.ShouldHaveSingleItem().ShouldSatisfyAllConditions(m =>
            {
                m.Id.Value.ShouldBe(teamId);
                m.Role.ToDto().ShouldBe(command.Role);
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