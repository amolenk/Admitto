using Amolenk.Admitto.Module.Organization.Application.Jobs;
using Amolenk.Admitto.Module.Organization.Application.Services;
using Amolenk.Admitto.Module.Organization.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Organization.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Amolenk.Admitto.Module.Organization.Tests.Application.Jobs;

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
            .WithEmailAddress(Amolenk.Admitto.Module.Shared.Kernel.ValueObjects.EmailAddress.From(EmailAddress))
            .Build();

        user.AssignExternalUserId(Amolenk.Admitto.Module.Organization.Domain.ValueObjects.ExternalUserId.From(ExternalUserId));

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.Users.Add(user);
        });

        UserId = user.Id.Value;

        // Set DeprovisionAfter to the past via raw SQL, bypassing the 7-day domain constraint.
        await environment.Database.Context.Database.ExecuteSqlAsync(
            $"UPDATE organization.users SET deprovision_after = NOW() - INTERVAL '1 hour' WHERE id = {user.Id.Value}");

        environment.Database.Context.ChangeTracker.Clear();
    }

    public DeprovisionUserIdpJob CreateJob(IntegrationTestEnvironment environment)
    {
        var unitOfWork = new DbContextUnitOfWork(environment.Database.Context);

        return new DeprovisionUserIdpJob(
            environment.Database.Context,
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
