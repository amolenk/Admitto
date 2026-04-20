using Amolenk.Admitto.Module.Registrations.Application.UseCases.CancellationPolicy.RemoveCancellationPolicy;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.CancellationPolicy;

[TestClass]
public sealed class RemoveCancellationPolicyTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC001: Remove existing policy → policy deleted from DB
    [TestMethod]
    public async ValueTask SC001_RemoveCancellationPolicy_PolicyExists_DeletesPolicy()
    {
        // Arrange
        var fixture = SetCancellationPolicyFixture.ActiveEventWithPolicy();
        await fixture.SetupAsync(Environment);

        var command = new RemoveCancellationPolicyCommand(fixture.EventId);
        var sut = new RemoveCancellationPolicyHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.CancellationPolicies
                .FirstOrDefaultAsync(p => p.Id == fixture.EventId, testContext.CancellationToken);
            policy.ShouldBeNull();
        });
    }

    // SC002: Remove when no policy exists → no-op (no error)
    [TestMethod]
    public async ValueTask SC002_RemoveCancellationPolicy_NoPolicyExists_NoOp()
    {
        // Arrange
        var fixture = SetCancellationPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new RemoveCancellationPolicyCommand(fixture.EventId);
        var sut = new RemoveCancellationPolicyHandler(Environment.Database.Context);

        // Act — should not throw
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.CancellationPolicies
                .FirstOrDefaultAsync(p => p.Id == fixture.EventId, testContext.CancellationToken);
            policy.ShouldBeNull();
        });
    }

    // SC003: Remove on cancelled event → throws EventNotActive
    [TestMethod]
    public async ValueTask SC003_RemoveCancellationPolicy_CancelledEvent_ThrowsEventNotActive()
    {
        // Arrange
        var fixture = SetCancellationPolicyFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var command = new RemoveCancellationPolicyCommand(fixture.EventId);
        var sut = new RemoveCancellationPolicyHandler(Environment.Database.Context);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(TicketedEventLifecycleGuard.Errors.EventNotActive);
    }
}
