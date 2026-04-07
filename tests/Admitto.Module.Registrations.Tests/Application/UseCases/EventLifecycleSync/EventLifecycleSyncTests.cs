using Amolenk.Admitto.Module.Registrations.Application.UseCases.EventLifecycleSync.HandleEventArchived;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.EventLifecycleSync.HandleEventCancelled;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.EventLifecycleSync;

[TestClass]
public sealed class EventLifecycleSyncTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: Cancel existing active policy — status becomes Cancelled
    [TestMethod]
    public async ValueTask SC001_HandleEventCancelled_ActivePolicy_StatusBecomesCancelled()
    {
        // Arrange
        var fixture = EventLifecycleSyncFixture.WithActivePolicy();
        await fixture.SetupAsync(Environment);

        var command = new HandleEventCancelledCommand(fixture.EventId.Value);
        var sut = new HandleEventCancelledHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.EventRegistrationPolicies
                .FirstOrDefaultAsync(p => p.Id == fixture.EventId, testContext.CancellationToken);

            policy.ShouldNotBeNull();
            policy.EventLifecycleStatus.ShouldBe(EventLifecycleStatus.Cancelled);
            policy.IsEventActive.ShouldBeFalse();
        });
    }

    // SC-002: Cancel with no existing policy — creates policy and sets Cancelled
    [TestMethod]
    public async ValueTask SC002_HandleEventCancelled_NoPolicyExists_CreatesPolicyAndSetsCancelled()
    {
        // Arrange
        var fixture = EventLifecycleSyncFixture.NoPolicyExists();
        await fixture.SetupAsync(Environment);

        var command = new HandleEventCancelledCommand(fixture.EventId.Value);
        var sut = new HandleEventCancelledHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.EventRegistrationPolicies
                .FirstOrDefaultAsync(p => p.Id == fixture.EventId, testContext.CancellationToken);

            policy.ShouldNotBeNull();
            policy.EventLifecycleStatus.ShouldBe(EventLifecycleStatus.Cancelled);
        });
    }

    // SC-003: Cancel is idempotent — no error on double cancel
    [TestMethod]
    public async ValueTask SC003_HandleEventCancelled_AlreadyCancelled_IdempotentNoError()
    {
        // Arrange
        var fixture = EventLifecycleSyncFixture.WithCancelledPolicy();
        await fixture.SetupAsync(Environment);

        var command = new HandleEventCancelledCommand(fixture.EventId.Value);
        var sut = new HandleEventCancelledHandler(Environment.Database.Context);

        // Act — should not throw
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.EventRegistrationPolicies
                .FirstOrDefaultAsync(p => p.Id == fixture.EventId, testContext.CancellationToken);

            policy.ShouldNotBeNull();
            policy.EventLifecycleStatus.ShouldBe(EventLifecycleStatus.Cancelled);
        });
    }

    // SC-004: Archive existing active policy — status becomes Archived
    [TestMethod]
    public async ValueTask SC004_HandleEventArchived_ActivePolicy_StatusBecomesArchived()
    {
        // Arrange
        var fixture = EventLifecycleSyncFixture.WithActivePolicy();
        await fixture.SetupAsync(Environment);

        var command = new HandleEventArchivedCommand(fixture.EventId.Value);
        var sut = new HandleEventArchivedHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.EventRegistrationPolicies
                .FirstOrDefaultAsync(p => p.Id == fixture.EventId, testContext.CancellationToken);

            policy.ShouldNotBeNull();
            policy.EventLifecycleStatus.ShouldBe(EventLifecycleStatus.Archived);
            policy.IsEventActive.ShouldBeFalse();
        });
    }

    // SC-005: Archive already-cancelled policy — status becomes Archived
    [TestMethod]
    public async ValueTask SC005_HandleEventArchived_CancelledPolicy_StatusBecomesArchived()
    {
        // Arrange
        var fixture = EventLifecycleSyncFixture.WithCancelledPolicy();
        await fixture.SetupAsync(Environment);

        var command = new HandleEventArchivedCommand(fixture.EventId.Value);
        var sut = new HandleEventArchivedHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.EventRegistrationPolicies
                .FirstOrDefaultAsync(p => p.Id == fixture.EventId, testContext.CancellationToken);

            policy.ShouldNotBeNull();
            policy.EventLifecycleStatus.ShouldBe(EventLifecycleStatus.Archived);
        });
    }

    // SC-006: Archive creates policy if none exists
    [TestMethod]
    public async ValueTask SC006_HandleEventArchived_NoPolicyExists_CreatesPolicyAndSetsArchived()
    {
        // Arrange
        var fixture = EventLifecycleSyncFixture.NoPolicyExists();
        await fixture.SetupAsync(Environment);

        var command = new HandleEventArchivedCommand(fixture.EventId.Value);
        var sut = new HandleEventArchivedHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.EventRegistrationPolicies
                .FirstOrDefaultAsync(p => p.Id == fixture.EventId, testContext.CancellationToken);

            policy.ShouldNotBeNull();
            policy.EventLifecycleStatus.ShouldBe(EventLifecycleStatus.Archived);
        });
    }
}
