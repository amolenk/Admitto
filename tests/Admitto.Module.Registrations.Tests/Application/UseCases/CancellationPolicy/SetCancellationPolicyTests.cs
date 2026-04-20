using Amolenk.Admitto.Module.Registrations.Application.UseCases.CancellationPolicy.SetCancellationPolicy;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.CancellationPolicy;

[TestClass]
public sealed class SetCancellationPolicyTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC001: Set cutoff on active event (no existing policy) → creates policy with correct cutoff
    [TestMethod]
    public async ValueTask SC001_SetCancellationPolicy_NoPolicyExists_CreatesPolicyWithCorrectCutoff()
    {
        // Arrange
        var fixture = SetCancellationPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var cutoff = new DateTimeOffset(2025, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var command = new SetCancellationPolicyCommand(fixture.EventId, cutoff);
        var sut = new SetCancellationPolicyHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.CancellationPolicies
                .FirstOrDefaultAsync(p => p.Id == fixture.EventId, testContext.CancellationToken);
            policy.ShouldNotBeNull();
            policy.LateCancellationCutoff.ShouldBe(cutoff);
        });
    }

    // SC002: Update cutoff on active event (policy exists) → cutoff updated
    [TestMethod]
    public async ValueTask SC002_SetCancellationPolicy_PolicyExists_UpdatesCutoff()
    {
        // Arrange
        var fixture = SetCancellationPolicyFixture.ActiveEventWithPolicy();
        await fixture.SetupAsync(Environment);

        var newCutoff = new DateTimeOffset(2025, 12, 1, 0, 0, 0, TimeSpan.Zero);
        var command = new SetCancellationPolicyCommand(fixture.EventId, newCutoff);
        var sut = new SetCancellationPolicyHandler(Environment.Database.Context);

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.CancellationPolicies
                .FirstOrDefaultAsync(p => p.Id == fixture.EventId, testContext.CancellationToken);
            policy.ShouldNotBeNull();
            policy.LateCancellationCutoff.ShouldBe(newCutoff);
        });
    }

    // SC003: Set cutoff on cancelled event → throws EventNotActive
    [TestMethod]
    public async ValueTask SC003_SetCancellationPolicy_CancelledEvent_ThrowsEventNotActive()
    {
        // Arrange
        var fixture = SetCancellationPolicyFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var cutoff = new DateTimeOffset(2025, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var command = new SetCancellationPolicyCommand(fixture.EventId, cutoff);
        var sut = new SetCancellationPolicyHandler(Environment.Database.Context);

        // Act
        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        // Assert
        result.Error.ShouldMatch(TicketedEventLifecycleGuard.Errors.EventNotActive);
    }
}
