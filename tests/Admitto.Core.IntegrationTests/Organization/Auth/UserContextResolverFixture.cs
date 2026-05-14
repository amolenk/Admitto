using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Domain;
using Amolenk.Admitto.ApiService.Auth;
using ExternalUserIdVO = Amolenk.Admitto.Core.Organization.Domain.ValueObjects.ExternalUserId;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Auth;

internal sealed class UserContextResolverFixture
{
    public const string UserEmail = "alice@example.com";
    public const string ExternalUserId = "auth0|abc123";
    public const string DisplayName = "Alice";

    public Guid UserId { get; private set; }

    public async ValueTask SeedUserWithoutExternalIdAsync(IntegrationTestEnvironment environment)
    {
        var user = new UserBuilder()
            .WithEmailAddress(EmailAddress.From(UserEmail))
            .Build();

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });

        UserId = user.Id.Value;
    }

    public async ValueTask SeedUserWithExternalIdAsync(IntegrationTestEnvironment environment)
    {
        var user = new UserBuilder()
            .WithEmailAddress(EmailAddress.From(UserEmail))
            .Build();

        user.AssignExternalUserId(ExternalUserIdVO.From(ExternalUserId));

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });

        UserId = user.Id.Value;
    }

    public UserContextResolver CreateResolver(IntegrationTestEnvironment environment)
        => new(
            environment.OrganizationDatabase.Context,
            new DbContextUnitOfWork(environment.OrganizationDatabase.Context));
}
