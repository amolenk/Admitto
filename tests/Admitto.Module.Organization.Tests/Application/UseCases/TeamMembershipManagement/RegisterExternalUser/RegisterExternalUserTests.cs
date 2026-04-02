using Amolenk.Admitto.Module.Organization.Application.Services;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.RegisterExternalUser;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.UseCases.TeamMembershipManagement.RegisterExternalUser;

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
        await Environment.Database.AssertAsync(async dbContext =>
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
        result.Error.ShouldMatch(NotFoundError.Create<User>(fixture.UserId));
    }

    private static RegisterExternalUserCommand NewRegisterExternalUserCommand(Guid userId)
    {
        return new RegisterExternalUserCommand(userId);
    }

    private static RegisterExternalUserHandler NewRegisterExternalUserHandler(
        IExternalUserDirectory externalUserDirectory) =>
        new(Environment.Database.Context, externalUserDirectory);
}