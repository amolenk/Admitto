using Amolenk.Admitto.Core.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamMemberships.BootstrapAdminUser;

[TestClass]
public sealed class BootstrapAdminUserInitializerTests : AspireIntegrationTestBase
{
    // Given a fresh database with no users
    // When the initializer starts
    // Then an Admin user is created for the configured email with a UserCreatedDomainEvent queued
    [TestMethod]
    public async Task FreshDatabase_CreatesAdminAndSendsInvitation()
    {
        // Arrange
        // On a fresh database with no users, the initializer creates an Admin user for the configured email.
        var fixture = BootstrapAdminUserInitializerFixture.Create(Environment);
        var sut = fixture.Initializer;

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert — check the newly tracked user entity exists and has a UserCreatedDomainEvent queued
        var trackedUser = Environment.OrganizationDatabase.Context.ChangeTracker.Entries<User>()
            .Select(e => e.Entity)
            .SingleOrDefault(u => u.EmailAddress.Value == BootstrapAdminUserInitializerFixture.AdminEmail);

        trackedUser.ShouldNotBeNull();
        trackedUser.ExternalUserId.ShouldBe(ExternalUserId.From(BootstrapAdminUserInitializerFixture.ExternalUserId));
        trackedUser.GetDomainEvents()
            .OfType<UserCreatedDomainEvent>()
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(e =>
            {
                e.UserId.ShouldBe(trackedUser.Id);
                e.EmailAddress.Value.ShouldBe(BootstrapAdminUserInitializerFixture.AdminEmail);
            });
    }

    // Given the admin user was already created and provisioned by a previous run
    // When the initializer starts again
    // Then no duplicate user or UserCreatedDomainEvent is created, but the user is still invited
    [TestMethod]
    public async Task AdminAlreadyExistsWithExternalUserId_SkipsCreationAndProvisioning()
    {
        // Arrange
        // On a restart when the admin user already exists, the initializer must not create a duplicate user.
        var fixture = BootstrapAdminUserInitializerFixture.Create(Environment);
        var sut = fixture.Initializer;

        // Run once to create the user
        await sut.StartAsync(CancellationToken.None);

        Environment.OrganizationDatabase.Context.ChangeTracker.Clear();

        // Act — run again to verify idempotency
        await sut.StartAsync(CancellationToken.None);

        // Assert — check that the loaded user entity doesn't have an UserCreatedDomainEvent enqueued
        var trackedUser = Environment.OrganizationDatabase.Context.ChangeTracker.Entries<User>()
            .Select(e => e.Entity)
            .SingleOrDefault(u => u.EmailAddress.Value == BootstrapAdminUserInitializerFixture.AdminEmail);

        trackedUser.ShouldNotBeNull();
        trackedUser.GetDomainEvents()
            .OfType<UserCreatedDomainEvent>()
            .ShouldBeEmpty();
        await fixture.ExternalUserDirectory.Received(1)
            .InviteUserAsync(BootstrapAdminUserInitializerFixture.AdminEmail, Arg.Any<CancellationToken>());
    }

    // Given an admin user already exists but has no external user ID
    // When the initializer starts
    // Then the user is provisioned and invited, and the external user ID is persisted
    [TestMethod]
    public async Task ExistingAdminWithoutExternalUserId_ProvisionsAndStoresExternalUserId()
    {
        var fixture = BootstrapAdminUserInitializerFixture.Create(Environment);
        var user = User.CreateAdmin(EmailAddress.From(BootstrapAdminUserInitializerFixture.AdminEmail));
        await Environment.OrganizationDatabase.SeedAsync(db => db.Users.Add(user));

        await fixture.Initializer.StartAsync(CancellationToken.None);

        await Environment.OrganizationDatabase.AssertAsync(db =>
        {
            var persisted = db.Users.Single(u => u.EmailAddress == EmailAddress.From(BootstrapAdminUserInitializerFixture.AdminEmail));
            persisted.ExternalUserId.ShouldBe(ExternalUserId.From(BootstrapAdminUserInitializerFixture.ExternalUserId));
            return ValueTask.CompletedTask;
        });
        await fixture.ExternalUserDirectory.Received(1)
            .InviteUserAsync(BootstrapAdminUserInitializerFixture.AdminEmail, Arg.Any<CancellationToken>());
    }
}
