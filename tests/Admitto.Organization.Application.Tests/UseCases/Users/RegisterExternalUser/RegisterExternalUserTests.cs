using Amolenk.Admitto.Organization.Application.Services;
using Amolenk.Admitto.Organization.Application.Tests.Infrastructure;
using Amolenk.Admitto.Organization.Application.UseCases.Users.RegisterExternalUser;
using Amolenk.Admitto.Organization.Domain.Entities;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;

namespace Amolenk.Admitto.Organization.Application.Tests.UseCases.Users.RegisterExternalUser;

[TestClass]
public sealed class RegisterExternalUserTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask RegisterExternalUser_ExternalUserDoesNotExist_RegistersExternalUser()
    {
        // Arrange
        var fixture = RegisterExternalUserFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var command = NewRegisterExternalUserCommand(fixture.UserId, fixture.EmailAddress);
        var sut = NewRegisterExternalUserHandler(fixture.ExternalUserDirectory);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            // Verify that the external user ID is added to the existing user.
            var user = await dbContext.Users.FindAsync([fixture.UserId], testContext.CancellationToken);

            user.ShouldNotBeNull();
            user.ExternalUserId.ShouldBe(fixture.ExternalUserId);
        });
    }

    [TestMethod]
    public async ValueTask RegisterExternalUser_UserDoesNotExist_ThrowsException()
    {
        // Arrange
        var fixture = RegisterExternalUserFixture.UserDoesNotExist();
        await fixture.SetupAsync(Environment);

        var command = NewRegisterExternalUserCommand(fixture.UserId, fixture.EmailAddress);
        var sut = NewRegisterExternalUserHandler(fixture.ExternalUserDirectory);

        // Act
        var result = await ErrorResult.CaptureAsync(() => sut.HandleAsync(command, testContext.CancellationToken));

        // Assert
        result.Error.ShouldMatch(NotFoundError.Create<User>(fixture.UserId));
    }

    private static RegisterExternalUserCommand NewRegisterExternalUserCommand(
        UserId userId,
        EmailAddress? emailAddress = null)
    {
        emailAddress ??= EmailAddress.From("alice@example.com");

        return new RegisterExternalUserCommand(userId, emailAddress.Value);
    }

    private static RegisterExternalUserHandler NewRegisterExternalUserHandler(
        IExternalUserDirectory externalUserDirectory) =>
        new(Environment.Database.Context, externalUserDirectory);
}