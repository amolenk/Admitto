using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.Bootstrap;

[TestClass]
public sealed class BootstrapAdminInitializerTests : AspireIntegrationTestBase
{
    [TestMethod]
    public async Task FreshDatabase_CreatesAdminAndSendsInvitation()
    {
        // Arrange
        // SC-BOOTSTRAP-NEW: On a fresh database with no users, the initializer creates an
        // Admin user for the configured email and issues a passkey-enrollment invitation.
        var fixture = new BootstrapAdminInitializerFixture();
        var sut = fixture.CreateInitializer(Environment);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert — user was invited exactly once
        await fixture.ExternalUserDirectory.Received(1)
            .InviteUserAsync(BootstrapAdminInitializerFixture.AdminEmail, Arg.Any<CancellationToken>());

        // Assert — user exists in the DB with Admin role and ExternalUserId bound
        await Environment.OrganizationDatabase.AssertAsync(async dbContext =>
        {
            var emailAddress = EmailAddress.From(BootstrapAdminInitializerFixture.AdminEmail);
            var user = await dbContext.Users.FirstOrDefaultAsync(
                u => u.EmailAddress == emailAddress);

            user.ShouldNotBeNull();
            user.IsAdmin.ShouldBeTrue();
            user.ExternalUserId.ShouldBe(
                ExternalUserId.From(BootstrapAdminInitializerFixture.FakeExternalUserId));
        });
    }

    [TestMethod]
    public async Task AdminAlreadyExists_SkipsCreationAndInvitation()
    {
        // Arrange
        // SC-BOOTSTRAP-IDEMPOTENT: On a restart when the admin user already exists and has an
        // ExternalUserId, the initializer must not create a duplicate user or send another invite.
        var fixture = new BootstrapAdminInitializerFixture();
        var sut = fixture.CreateInitializer(Environment);

        // Run once to create the user
        await sut.StartAsync(CancellationToken.None);

        Environment.OrganizationDatabase.Context.ChangeTracker.Clear();

        // Act — run again to verify idempotency
        await sut.StartAsync(CancellationToken.None);

        // Assert — InviteUserAsync was called only once across both runs
        await fixture.ExternalUserDirectory.Received(1)
            .InviteUserAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
