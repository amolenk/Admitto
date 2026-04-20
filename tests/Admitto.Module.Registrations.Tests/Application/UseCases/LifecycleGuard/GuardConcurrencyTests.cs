using Amolenk.Admitto.Module.Registrations.Application.Services;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Interceptors;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.TestContexts;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.LifecycleGuard;

/// <summary>
/// Verifies the guard-pattern concurrency contract: when a policy-mutation and a
/// lifecycle-event both read the guard row, the second writer receives a
/// <see cref="DbUpdateConcurrencyException"/>.
/// </summary>
[TestClass]
public sealed class GuardConcurrencyTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC001_ConcurrentPolicyMutationAndLifecycleEvent_SecondWriterGetsConflict()
    {
        // Arrange: seed an active guard.
        var eventId = TicketedEventId.New();
        await Environment.Database.SeedAsync(dbContext =>
        {
            dbContext.TicketedEventLifecycleGuards.Add(
                TicketedEventLifecycleGuard.Create(eventId));
        });

        // Create a second, independent DbContext pointing at the same database so
        // both "transactions" read the same xmin value for the guard row.
        await using var secondContext = CreateIndependentContext();

        // --- Reader 1 (policy mutation): loads guard in the primary context ---
        var guard1 = await LifecycleGuardStore.LoadOrCreateAsync(
            Environment.Database.Context, eventId, testContext.CancellationToken);
        guard1.AssertActiveAndRegisterPolicyMutation();

        // --- Reader 2 (lifecycle event): loads the same guard in a separate context ---
        var guard2 = await LifecycleGuardStore.LoadOrCreateAsync(
            secondContext, eventId, testContext.CancellationToken);
        guard2.SetCancelled();

        // Writer 1 commits first — succeeds.
        await Environment.Database.Context.SaveChangesAsync(testContext.CancellationToken);

        // Writer 2 commits second — must fail because guard.Version (xmin) changed.
        await Should.ThrowAsync<DbUpdateConcurrencyException>(
            () => secondContext.SaveChangesAsync(testContext.CancellationToken));
    }

    private RegistrationsDbContext CreateIndependentContext()
    {
        var connectionString = Environment.Database.Context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Connection string not available from test context.");

        var options = new DbContextOptionsBuilder<RegistrationsDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("ef_migrations_history", RegistrationsDbContext.SchemaName))
            .AddInterceptors(new AuditInterceptor(new FakeUserContextAccessor()))
            .Options;

        return new RegistrationsDbContext(options);
    }
}
