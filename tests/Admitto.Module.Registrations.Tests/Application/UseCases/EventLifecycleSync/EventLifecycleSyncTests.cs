using Amolenk.Admitto.Module.Registrations.Application.UseCases.EventLifecycleSync.HandleEventArchived;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.EventLifecycleSync.HandleEventCancelled;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.EventLifecycleSync.HandleEventCreated;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.EventLifecycleSync;

[TestClass]
public sealed class EventLifecycleSyncTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: Cancel existing active guard — status becomes Cancelled
    [TestMethod]
    public async ValueTask SC001_HandleEventCancelled_ActiveGuard_StatusBecomesCancelled()
    {
        var fixture = EventLifecycleSyncFixture.WithActiveGuard();
        await fixture.SetupAsync(Environment);

        var command = new HandleEventCancelledCommand(fixture.EventId.Value);
        var sut = new HandleEventCancelledHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var guard = await dbContext.TicketedEventLifecycleGuards
                .FirstOrDefaultAsync(g => g.Id == fixture.EventId, testContext.CancellationToken);

            guard.ShouldNotBeNull();
            guard.LifecycleStatus.ShouldBe(EventLifecycleStatus.Cancelled);
            guard.IsActive.ShouldBeFalse();
        });
    }

    // SC-002: Cancel with no existing guard — creates guard and sets Cancelled
    [TestMethod]
    public async ValueTask SC002_HandleEventCancelled_NoGuardExists_CreatesGuardAndSetsCancelled()
    {
        var fixture = EventLifecycleSyncFixture.NoGuardExists();
        await fixture.SetupAsync(Environment);

        var command = new HandleEventCancelledCommand(fixture.EventId.Value);
        var sut = new HandleEventCancelledHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var guard = await dbContext.TicketedEventLifecycleGuards
                .FirstOrDefaultAsync(g => g.Id == fixture.EventId, testContext.CancellationToken);

            guard.ShouldNotBeNull();
            guard.LifecycleStatus.ShouldBe(EventLifecycleStatus.Cancelled);
        });
    }

    // SC-003: Cancel is idempotent — no error on double cancel
    [TestMethod]
    public async ValueTask SC003_HandleEventCancelled_AlreadyCancelled_IdempotentNoError()
    {
        var fixture = EventLifecycleSyncFixture.WithCancelledGuard();
        await fixture.SetupAsync(Environment);

        var command = new HandleEventCancelledCommand(fixture.EventId.Value);
        var sut = new HandleEventCancelledHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var guard = await dbContext.TicketedEventLifecycleGuards
                .FirstOrDefaultAsync(g => g.Id == fixture.EventId, testContext.CancellationToken);

            guard.ShouldNotBeNull();
            guard.LifecycleStatus.ShouldBe(EventLifecycleStatus.Cancelled);
        });
    }

    // SC-004: Archive existing active guard — status becomes Archived
    [TestMethod]
    public async ValueTask SC004_HandleEventArchived_ActiveGuard_StatusBecomesArchived()
    {
        var fixture = EventLifecycleSyncFixture.WithActiveGuard();
        await fixture.SetupAsync(Environment);

        var command = new HandleEventArchivedCommand(fixture.EventId.Value);
        var sut = new HandleEventArchivedHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var guard = await dbContext.TicketedEventLifecycleGuards
                .FirstOrDefaultAsync(g => g.Id == fixture.EventId, testContext.CancellationToken);

            guard.ShouldNotBeNull();
            guard.LifecycleStatus.ShouldBe(EventLifecycleStatus.Archived);
            guard.IsActive.ShouldBeFalse();
        });
    }

    // SC-005: Archive already-cancelled guard — status becomes Archived
    [TestMethod]
    public async ValueTask SC005_HandleEventArchived_CancelledGuard_StatusBecomesArchived()
    {
        var fixture = EventLifecycleSyncFixture.WithCancelledGuard();
        await fixture.SetupAsync(Environment);

        var command = new HandleEventArchivedCommand(fixture.EventId.Value);
        var sut = new HandleEventArchivedHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var guard = await dbContext.TicketedEventLifecycleGuards
                .FirstOrDefaultAsync(g => g.Id == fixture.EventId, testContext.CancellationToken);

            guard.ShouldNotBeNull();
            guard.LifecycleStatus.ShouldBe(EventLifecycleStatus.Archived);
        });
    }

    // SC-006: Archive creates guard if none exists
    [TestMethod]
    public async ValueTask SC006_HandleEventArchived_NoGuardExists_CreatesGuardAndSetsArchived()
    {
        var fixture = EventLifecycleSyncFixture.NoGuardExists();
        await fixture.SetupAsync(Environment);

        var command = new HandleEventArchivedCommand(fixture.EventId.Value);
        var sut = new HandleEventArchivedHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var guard = await dbContext.TicketedEventLifecycleGuards
                .FirstOrDefaultAsync(g => g.Id == fixture.EventId, testContext.CancellationToken);

            guard.ShouldNotBeNull();
            guard.LifecycleStatus.ShouldBe(EventLifecycleStatus.Archived);
        });
    }

    // SC-007: Created event syncs into Registrations as a fresh Active guard
    [TestMethod]
    public async ValueTask SC007_HandleEventCreated_NoGuardExists_CreatesActiveGuard()
    {
        var fixture = EventLifecycleSyncFixture.NoGuardExists();
        await fixture.SetupAsync(Environment);

        var command = new HandleEventCreatedCommand(fixture.EventId.Value);
        var sut = new HandleEventCreatedHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var guard = await dbContext.TicketedEventLifecycleGuards
                .FirstOrDefaultAsync(g => g.Id == fixture.EventId, testContext.CancellationToken);

            guard.ShouldNotBeNull();
            guard.LifecycleStatus.ShouldBe(EventLifecycleStatus.Active);
            guard.IsActive.ShouldBeTrue();
        });
    }

    // SC-008: Re-delivery of the created event is idempotent
    [TestMethod]
    public async ValueTask SC008_HandleEventCreated_GuardAlreadyExists_NoOp()
    {
        var fixture = EventLifecycleSyncFixture.WithActiveGuard();
        await fixture.SetupAsync(Environment);

        var command = new HandleEventCreatedCommand(fixture.EventId.Value);
        var sut = new HandleEventCreatedHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var guards = await dbContext.TicketedEventLifecycleGuards
                .Where(g => g.Id == fixture.EventId)
                .ToListAsync(testContext.CancellationToken);

            guards.Count.ShouldBe(1);
            guards[0].LifecycleStatus.ShouldBe(EventLifecycleStatus.Active);
        });
    }
}
