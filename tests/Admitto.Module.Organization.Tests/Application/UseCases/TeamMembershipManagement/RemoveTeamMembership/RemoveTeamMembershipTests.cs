using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.RemoveTeamMembership;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamMembershipManagement.RemoveTeamMembership;

[TestClass]
public sealed class RemoveTeamMembershipTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC008_RemoveTeamMembership_UserHasOtherTeams_RemovesMembership()
    {
        // Arrange
        // SC-008: Given alice is a member of two teams, when her membership in one team
        // is removed, only that membership is removed and she stays in the system.
        var fixture = RemoveTeamMembershipFixture.MemberWithOtherTeams();
        await fixture.SetupAsync(Environment);

        var command = new RemoveTeamMembershipCommand(fixture.TeamId, fixture.EmailAddress);
        var sut = new RemoveTeamMembershipHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var user = await dbContext.Users.FindAsync(
                [UserId.From(fixture.UserId)], testContext.CancellationToken);

            user.ShouldNotBeNull();
            user.Memberships.ShouldNotContain(m => m.Id.Value == fixture.TeamId);
            user.Memberships.ShouldContain(m => m.Id.Value == fixture.OtherTeamId);
            user.DeprovisionAfter.ShouldBeNull();
        });
    }

    [TestMethod]
    public async ValueTask SC009_RemoveTeamMembership_UserNotTeamMember_ThrowsError()
    {
        // Arrange
        // SC-009: Given alice is not a member of the target team, when someone attempts
        // to remove her membership, the request is rejected.
        var fixture = RemoveTeamMembershipFixture.MemberWithOtherTeams();
        await fixture.SetupAsync(Environment);

        var nonMemberTeamId = Guid.NewGuid();
        var command = new RemoveTeamMembershipCommand(nonMemberTeamId, fixture.EmailAddress);
        var sut = new RemoveTeamMembershipHandler(Environment.Database.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(
            User.Errors.UserNotTeamMember(
                UserId.From(fixture.UserId),
                TeamId.From(nonMemberTeamId)));
    }

    [TestMethod]
    public async ValueTask SC011_RemoveTeamMembership_LastMembership_SetsDeprovisionAfter()
    {
        // Arrange
        // SC-011: Given alice is a member of only one team, when her membership is removed,
        // DeprovisionAfter is set to approximately now + 7 days.
        var fixture = RemoveTeamMembershipFixture.MemberInOnlyThisTeam();
        await fixture.SetupAsync(Environment);

        var before = DateTimeOffset.UtcNow;
        var command = new RemoveTeamMembershipCommand(fixture.TeamId, fixture.EmailAddress);
        var sut = new RemoveTeamMembershipHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
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
