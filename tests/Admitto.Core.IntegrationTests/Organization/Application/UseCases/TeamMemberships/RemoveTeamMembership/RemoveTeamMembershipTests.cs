using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.RemoveTeamMembership;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamMemberships.RemoveTeamMembership;

[TestClass]
public sealed class RemoveTeamMembershipTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a user who is a member of two teams
    // When her membership in one team is removed
    // Then only that membership is removed and she remains a member of the other team
    [TestMethod]
    public async ValueTask RemoveTeamMembership_UserHasOtherTeams_RemovesMembership()
    {
        // Arrange
        var fixture = RemoveTeamMembershipFixture.MemberWithOtherTeams();
        await fixture.SetupAsync(Environment);

        var command = new RemoveTeamMembershipCommand(fixture.TeamId, fixture.EmailAddress);
        var sut = new RemoveTeamMembershipHandler(Environment.OrganizationDatabase.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.OrganizationDatabase.AssertAsync(async dbContext =>
        {
            var user = await dbContext.Users.FindAsync(
                [UserId.From(fixture.UserId)], testContext.CancellationToken);

            user.ShouldNotBeNull();
            user.Memberships.ShouldNotContain(m => m.TeamId.Value == fixture.TeamId);
            user.Memberships.ShouldContain(m => m.TeamId.Value == fixture.OtherTeamId);
            user.DeprovisionAfter.ShouldBeNull();
        });
    }

    // Given a user who is not a member of the target team
    // When removal of her membership in that team is attempted
    // Then the request is rejected with a user-not-team-member error
    [TestMethod]
    public async ValueTask RemoveTeamMembership_UserNotTeamMember_ThrowsError()
    {
        // Arrange
        var fixture = RemoveTeamMembershipFixture.MemberWithOtherTeams();
        await fixture.SetupAsync(Environment);

        var nonMemberTeamId = Guid.NewGuid();
        var command = new RemoveTeamMembershipCommand(nonMemberTeamId, fixture.EmailAddress);
        var sut = new RemoveTeamMembershipHandler(Environment.OrganizationDatabase.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(
            User.Errors.UserNotTeamMember(
                UserId.From(fixture.UserId),
                TeamId.From(nonMemberTeamId)));
    }

    // Given a user who is a member of only one team
    // When her membership is removed
    // Then her memberships become empty and DeprovisionAfter is set to approximately seven days from now
    [TestMethod]
    public async ValueTask RemoveTeamMembership_LastMembership_SetsDeprovisionAfter()
    {
        // Arrange
        var fixture = RemoveTeamMembershipFixture.MemberInOnlyThisTeam();
        await fixture.SetupAsync(Environment);

        var before = DateTimeOffset.UtcNow;
        var command = new RemoveTeamMembershipCommand(fixture.TeamId, fixture.EmailAddress);
        var sut = new RemoveTeamMembershipHandler(Environment.OrganizationDatabase.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.OrganizationDatabase.AssertAsync(async dbContext =>
        {
            var user = await dbContext.Users.FindAsync(
                [UserId.From(fixture.UserId)], testContext.CancellationToken);

            user.ShouldNotBeNull();
            user.Memberships.ShouldBeEmpty();
            user.DeprovisionAfter.ShouldNotBeNull();
            user.DeprovisionAfter!.Value.ShouldBeGreaterThan(before.AddDays(6));
            user.DeprovisionAfter.Value.ShouldBeLessThan(before.AddDays(8));
        });
    }
}
