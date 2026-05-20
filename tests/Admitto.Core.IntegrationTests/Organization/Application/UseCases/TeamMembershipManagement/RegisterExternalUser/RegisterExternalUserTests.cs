using Amolenk.Admitto.Core.Organization.Application.Services;
using Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMembershipManagement.RegisterExternalUser;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamMembershipManagement.RegisterExternalUser;

[TestClass]
public sealed class RegisterExternalUserTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask RegisterExternalUser_ExternalUserDoesNotExist_RegistersExternalUser()
    {
        // Arrange
        var fixture = RegisterExternalUserFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var command = NewRegisterExternalUserCommand(fixture.UserId);
        var sut = NewRegisterExternalUserHandler(fixture.ExternalUserDirectory);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.OrganizationDatabase.AssertAsync(async dbContext =>
        {
            // Verify that the external user ID is added to the existing user.
            var user = await dbContext.Users.FindAsync([UserId.From(fixture.UserId)], testContext.CancellationToken);

            user.ShouldNotBeNull();
            user.ExternalUserId.ShouldNotBeNull().Value.ShouldBe(fixture.ExternalUserId);
        });
    }

    [TestMethod]
    public async ValueTask RegisterExternalUser_UserDoesNotExist_ThrowsException()
    {
        // Arrange
        var fixture = RegisterExternalUserFixture.UserDoesNotExist();
        await fixture.SetupAsync(Environment);

        var command = NewRegisterExternalUserCommand(fixture.UserId);
        var sut = NewRegisterExternalUserHandler(fixture.ExternalUserDirectory);

        // Act
        var result = await ErrorResult.CaptureAsync(() => sut.HandleAsync(command, testContext.CancellationToken));

        // Assert
        result.Error.ShouldMatch(NotFoundError.Create<User>());
    }

    private static RegisterExternalUserCommand NewRegisterExternalUserCommand(Guid userId)
    {
        return new RegisterExternalUserCommand(userId);
    }

    private static RegisterExternalUserHandler NewRegisterExternalUserHandler(
        IExternalUserDirectory externalUserDirectory) =>
        new(Environment.OrganizationDatabase.Context, externalUserDirectory);
}
