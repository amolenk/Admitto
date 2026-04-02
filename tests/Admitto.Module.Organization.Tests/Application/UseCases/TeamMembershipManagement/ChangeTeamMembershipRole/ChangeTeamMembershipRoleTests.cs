using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.ChangeTeamMembershipRole;
using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamMembershipManagement.ChangeTeamMembershipRole;

[TestClass]
public sealed class ChangeTeamMembershipRoleTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC006_ChangeTeamMembershipRole_MemberExists_UpdatesRole()
    {
        // Arrange
        // SC-006: Given alice is a Crew member of the team, when the owner changes her
        // role to Organizer, her membership role is updated.
        var fixture = ChangeTeamMembershipRoleFixture.MemberExists();
        await fixture.SetupAsync(Environment);

        var command = new ChangeTeamMembershipRoleCommand(fixture.TeamId, fixture.EmailAddress, TeamMembershipRoleDto.Organizer);
        var sut = new ChangeTeamMembershipRoleHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var user = await dbContext.Users.FindAsync(
                [UserId.From(fixture.UserId)], testContext.CancellationToken);

            user.ShouldNotBeNull();
            var membership = user.Memberships.SingleOrDefault(m => m.Id.Value == fixture.TeamId);
            membership.ShouldNotBeNull();
            membership.Role.ShouldBe(TeamMembershipRole.Organizer);
        });
    }

    [TestMethod]
    public async ValueTask SC007_ChangeTeamMembershipRole_UserNotTeamMember_ThrowsError()
    {
        // Arrange
        // SC-007: Given alice has no membership in the specified team, when someone
        // attempts to change her role, the request is rejected.
        var fixture = ChangeTeamMembershipRoleFixture.MemberExists();
        await fixture.SetupAsync(Environment);

        var differentTeamId = Guid.NewGuid();
        var command = new ChangeTeamMembershipRoleCommand(differentTeamId, fixture.EmailAddress, TeamMembershipRoleDto.Organizer);
        var sut = new ChangeTeamMembershipRoleHandler(Environment.Database.Context);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(
            User.Errors.UserNotTeamMember(
                UserId.From(fixture.UserId),
                TeamId.From(differentTeamId)));
    }
}
