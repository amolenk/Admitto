using Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.RegistrationPolicy;

[TestClass]
public sealed class SetRegistrationPolicyTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC001: Configure registration window — policy already exists (synced from Organization),
    // window is updated correctly.
    [TestMethod]
    public async ValueTask SC001_SetRegistrationPolicy_ExistingPolicy_SetsWindowCorrectly()
    {
        var eventId = TicketedEventId.New();
        var opensAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var closesAt = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

        await Environment.Database.SeedAsync(dbContext =>
        {
            dbContext.EventRegistrationPolicies.Add(EventRegistrationPolicy.Create(eventId));
        });

        var command = new SetRegistrationPolicyCommand(eventId, opensAt, closesAt, null);
        var sut = new SetRegistrationPolicyHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.EventRegistrationPolicies.SingleOrDefaultAsync(testContext.CancellationToken);
            policy.ShouldNotBeNull();
            policy.Id.ShouldBe(eventId);
            policy.RegistrationWindowOpensAt.ShouldBe(opensAt);
            policy.RegistrationWindowClosesAt.ShouldBe(closesAt);
            policy.HasRegistrationWindow.ShouldBeTrue();
        });
    }

    // SC002: Update existing registration window — policy updated
    [TestMethod]
    public async ValueTask SC002_SetRegistrationPolicy_ExistingPolicy_UpdatesWindow()
    {
        var eventId = TicketedEventId.New();
        var initialOpens = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var initialCloses = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

        // Seed initial policy.
        await Environment.Database.SeedAsync(dbContext =>
        {
            var policy = EventRegistrationPolicy.Create(eventId);
            policy.SetWindow(initialOpens, initialCloses);
            dbContext.EventRegistrationPolicies.Add(policy);
        });

        var newOpens = new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var newCloses = new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero);

        var command = new SetRegistrationPolicyCommand(eventId, newOpens, newCloses, null);
        var sut = new SetRegistrationPolicyHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.EventRegistrationPolicies.SingleOrDefaultAsync(testContext.CancellationToken);
            policy.ShouldNotBeNull();
            policy.RegistrationWindowOpensAt.ShouldBe(newOpens);
            policy.RegistrationWindowClosesAt.ShouldBe(newCloses);
        });
    }

    // SC003: Set domain restriction — AllowedEmailDomain saved
    [TestMethod]
    public async ValueTask SC003_SetRegistrationPolicy_WithDomainRestriction_SavesDomain()
    {
        var eventId = TicketedEventId.New();
        var opensAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var closesAt = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

        await Environment.Database.SeedAsync(dbContext =>
        {
            dbContext.EventRegistrationPolicies.Add(EventRegistrationPolicy.Create(eventId));
        });

        var command = new SetRegistrationPolicyCommand(eventId, opensAt, closesAt, "@acme.com");
        var sut = new SetRegistrationPolicyHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.EventRegistrationPolicies.SingleOrDefaultAsync(testContext.CancellationToken);
            policy.ShouldNotBeNull();
            policy.AllowedEmailDomain.ShouldBe("@acme.com");
        });
    }

    // SC004: Remove domain restriction — AllowedEmailDomain cleared
    [TestMethod]
    public async ValueTask SC004_SetRegistrationPolicy_RemoveDomainRestriction_ClearsDomain()
    {
        var eventId = TicketedEventId.New();
        var opensAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var closesAt = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

        // Seed with domain restriction.
        await Environment.Database.SeedAsync(dbContext =>
        {
            var policy = EventRegistrationPolicy.Create(eventId);
            policy.SetWindow(opensAt, closesAt);
            policy.SetDomainRestriction("@acme.com");
            dbContext.EventRegistrationPolicies.Add(policy);
        });

        var command = new SetRegistrationPolicyCommand(eventId, opensAt, closesAt, null);
        var sut = new SetRegistrationPolicyHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.EventRegistrationPolicies.SingleOrDefaultAsync(testContext.CancellationToken);
            policy.ShouldNotBeNull();
            policy.AllowedEmailDomain.ShouldBeNull();
        });
    }

    // SC005: No window times → window cleared
    [TestMethod]
    public async ValueTask SC005_SetRegistrationPolicy_NullWindowTimes_ClearsWindow()
    {
        var eventId = TicketedEventId.New();
        var opensAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var closesAt = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

        // Seed with a window.
        await Environment.Database.SeedAsync(dbContext =>
        {
            var policy = EventRegistrationPolicy.Create(eventId);
            policy.SetWindow(opensAt, closesAt);
            dbContext.EventRegistrationPolicies.Add(policy);
        });

        var command = new SetRegistrationPolicyCommand(eventId, null, null, null);
        var sut = new SetRegistrationPolicyHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.EventRegistrationPolicies.SingleOrDefaultAsync(testContext.CancellationToken);
            policy.ShouldNotBeNull();
            policy.HasRegistrationWindow.ShouldBeFalse();
        });
    }

    // SC006: Invalid window (close before open) → domain exception from SetWindow
    [TestMethod]
    public async ValueTask SC006_SetRegistrationPolicy_CloseBeforeOpen_ThrowsWindowCloseBeforeOpenError()
    {
        var eventId = TicketedEventId.New();
        var opensAt = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var closesAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero); // before open

        await Environment.Database.SeedAsync(dbContext =>
        {
            dbContext.EventRegistrationPolicies.Add(EventRegistrationPolicy.Create(eventId));
        });

        var command = new SetRegistrationPolicyCommand(eventId, opensAt, closesAt, null);
        var sut = new SetRegistrationPolicyHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(EventRegistrationPolicy.Errors.WindowCloseBeforeOpen);
    }

    // SC007: No policy exists — handler auto-creates guard and policy, succeeds
    [TestMethod]
    public async ValueTask SC007_SetRegistrationPolicy_NoPolicy_CreatesGuardAndPolicy()
    {
        var eventId = TicketedEventId.New();
        var opensAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var closesAt = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var command = new SetRegistrationPolicyCommand(eventId, opensAt, closesAt, null);
        var sut = new SetRegistrationPolicyHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var guard = await dbContext.TicketedEventLifecycleGuards
                .SingleOrDefaultAsync(g => g.Id == eventId, testContext.CancellationToken);
            guard.ShouldNotBeNull();
            guard.IsActive.ShouldBeTrue();

            var policy = await dbContext.EventRegistrationPolicies
                .SingleOrDefaultAsync(p => p.Id == eventId, testContext.CancellationToken);
            policy.ShouldNotBeNull();
            policy.RegistrationWindowOpensAt.ShouldBe(opensAt);
            policy.RegistrationWindowClosesAt.ShouldBe(closesAt);
        });
    }
}
