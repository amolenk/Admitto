using Amolenk.Admitto.Core.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Core.Organization.Domain.Entities;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.UseCases.TeamMemberships.BootstrapAdminUser;

[TestClass]
public sealed class BootstrapAdminUserInitializerTests : AspireIntegrationTestBase
{
    [TestMethod]
    public async Task FreshDatabase_CreatesAdminAndSendsInvitation()
    {
        // Arrange
        // On a fresh database with no users, the initializer creates an Admin user for the configured email.
        var sut = BootstrapAdminUserInitializerFixture.CreateInitializer(Environment);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert — check the newly tracked user entity exists and has a UserCreatedDomainEvent queued
        var trackedUser = Environment.OrganizationDatabase.Context.ChangeTracker.Entries<User>()
            .Select(e => e.Entity)
            .SingleOrDefault(u => u.EmailAddress.Value == BootstrapAdminUserInitializerFixture.AdminEmail);

        trackedUser.ShouldNotBeNull();
        trackedUser.GetDomainEvents()
            .OfType<UserCreatedDomainEvent>()
            .ShouldHaveSingleItem()
            .ShouldSatisfyAllConditions(e =>
            {
                e.UserId.ShouldBe(trackedUser.Id);
                e.EmailAddress.Value.ShouldBe(BootstrapAdminUserInitializerFixture.AdminEmail);
            });
    }

    [TestMethod]
    public async Task AdminAlreadyExists_SkipsCreation()
    {
        // Arrange
        // On a restart when the admin user already exists, the initializer must not create a duplicate user.
        var sut = BootstrapAdminUserInitializerFixture.CreateInitializer(Environment);

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
    }
}
