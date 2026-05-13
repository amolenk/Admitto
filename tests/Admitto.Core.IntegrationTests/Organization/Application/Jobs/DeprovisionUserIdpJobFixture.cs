using Amolenk.Admitto.Core.Organization.Application.Jobs;
using Amolenk.Admitto.Core.Organization.Application.Services;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Testing.Builders.Organization.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Application.Jobs;

internal sealed class DeprovisionUserIdpJobFixture
{
    public string EmailAddress { get; } = "alice@example.com";
    public Guid ExternalUserId { get; } = Guid.NewGuid();
    public Guid UserId { get; private set; }

    public IExternalUserDirectory ExternalUserDirectory { get; } = Substitute.For<IExternalUserDirectory>();

    private DeprovisionUserIdpJobFixture()
    {
    }

    public static DeprovisionUserIdpJobFixture GracePeriodExpired() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var user = new UserBuilder()
            .WithEmailAddress(Amolenk.Admitto.Core.Shared.Kernel.ValueObjects.EmailAddress.From(EmailAddress))
            .Build();

        user.AssignExternalUserId(Amolenk.Admitto.Core.Organization.Domain.ValueObjects.ExternalUserId.From(ExternalUserId));

        await environment.OrganizationDatabase.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });

        UserId = user.Id.Value;

        // Set DeprovisionAfter to the past via raw SQL, bypassing the 7-day domain constraint.
        await environment.OrganizationDatabase.Context.Database.ExecuteSqlAsync(
            $"UPDATE organization.users SET deprovision_after = NOW() - INTERVAL '1 hour' WHERE id = {user.Id.Value}");

        environment.OrganizationDatabase.Context.ChangeTracker.Clear();
    }

    public DeprovisionUserIdpJob CreateJob(IntegrationTestEnvironment environment)
    {
        var unitOfWork = new DbContextUnitOfWork(environment.OrganizationDatabase.Context);

        return new DeprovisionUserIdpJob(
            environment.OrganizationDatabase.Context,
            ExternalUserDirectory,
            unitOfWork,
            NullLogger<DeprovisionUserIdpJob>.Instance);
    }
}

/// <summary>
/// Simple IUnitOfWork adapter that delegates SaveChangesAsync to the underlying DbContext.
/// Used in tests to avoid the full DI infrastructure needed for keyed service injection.
/// </summary>
internal sealed class DbContextUnitOfWork(DbContext context) : IUnitOfWork
{
    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
