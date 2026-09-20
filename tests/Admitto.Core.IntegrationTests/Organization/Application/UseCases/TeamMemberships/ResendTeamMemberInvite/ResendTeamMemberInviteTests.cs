using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.ResendTeamMemberInvite;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using NSubstitute;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamMemberships.ResendTeamMemberInvite;

[TestClass]
public sealed class ResendTeamMemberInviteTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given alice is a member of the team
    // When an owner resends her invite
    // Then the external user directory is asked to (re-)invite her by email
    [TestMethod]
    public async ValueTask ResendTeamMemberInvite_MemberExists_InvitesUserAgain()
    {
        // Arrange
        var fixture = ResendTeamMemberInviteFixture.MemberOfTheTeam();
        await fixture.SetupAsync(Environment);

        var command = new ResendTeamMemberInviteCommand(fixture.TeamId, fixture.EmailAddress);
        var sut = new ResendTeamMemberInviteHandler(
            Environment.OrganizationDatabase.Context,
            fixture.ExternalUserDirectory);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await fixture.ExternalUserDirectory
            .Received(1)
            .InviteUserAsync(fixture.EmailAddress, Arg.Any<CancellationToken>());
    }

    // Given alice has no membership in the specified team
    // When someone attempts to resend her invite for that team
    // Then a business rule error for a user who is not a team member is thrown
    [TestMethod]
    public async ValueTask ResendTeamMemberInvite_UserNotTeamMember_ThrowsError()
    {
        // Arrange
        var fixture = ResendTeamMemberInviteFixture.MemberOfAnotherTeamOnly();
        await fixture.SetupAsync(Environment);

        var command = new ResendTeamMemberInviteCommand(fixture.TeamId, fixture.EmailAddress);
        var sut = new ResendTeamMemberInviteHandler(
            Environment.OrganizationDatabase.Context,
            fixture.ExternalUserDirectory);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(
            User.Errors.UserNotTeamMember(
                UserId.From(fixture.UserId),
                TeamId.From(fixture.TeamId)));

        await fixture.ExternalUserDirectory
            .DidNotReceive()
            .InviteUserAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // Given no user exists with the given email address
    // When someone attempts to resend an invite for that email
    // Then a not-found error is thrown
    [TestMethod]
    public async ValueTask ResendTeamMemberInvite_UserDoesNotExist_ThrowsError()
    {
        // Arrange
        var fixture = ResendTeamMemberInviteFixture.UserDoesNotExist();
        await fixture.SetupAsync(Environment);

        var command = new ResendTeamMemberInviteCommand(fixture.TeamId, fixture.EmailAddress);
        var sut = new ResendTeamMemberInviteHandler(
            Environment.OrganizationDatabase.Context,
            fixture.ExternalUserDirectory);

        // Act & Assert
        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(
            TeamMembershipErrors.UserNotFound(EmailAddress.From(fixture.EmailAddress)));

        await fixture.ExternalUserDirectory
            .DidNotReceive()
            .InviteUserAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
