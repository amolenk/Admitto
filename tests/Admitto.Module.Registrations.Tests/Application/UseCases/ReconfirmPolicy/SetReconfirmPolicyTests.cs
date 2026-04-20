using Amolenk.Admitto.Module.Registrations.Application.UseCases.ReconfirmPolicy.SetReconfirmPolicy;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.ReconfirmPolicy;

[TestClass]
public sealed class SetReconfirmPolicyTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC001_SetReconfirmPolicy_NoExistingPolicy_CreatesPolicy()
    {
        var fixture = SetReconfirmPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var opensAt = new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var closesAt = new DateTimeOffset(2025, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var cadence = TimeSpan.FromDays(3);

        var command = new SetReconfirmPolicyCommand(fixture.EventId, opensAt, closesAt, cadence);
        var sut = new SetReconfirmPolicyHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.ReconfirmPolicies
                .FirstOrDefaultAsync(p => p.Id == fixture.EventId, testContext.CancellationToken);

            policy.ShouldNotBeNull();
            policy.OpensAt.ShouldBe(opensAt);
            policy.ClosesAt.ShouldBe(closesAt);
            policy.Cadence.ShouldBe(cadence);
        });
    }

    [TestMethod]
    public async ValueTask SC002_SetReconfirmPolicy_ExistingPolicy_UpdatesAllFields()
    {
        var fixture = SetReconfirmPolicyFixture.ActiveEventWithExistingPolicy();
        await fixture.SetupAsync(Environment);

        var newOpensAt = new DateTimeOffset(2025, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var newClosesAt = new DateTimeOffset(2025, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var newCadence = TimeSpan.FromDays(14);

        var command = new SetReconfirmPolicyCommand(fixture.EventId, newOpensAt, newClosesAt, newCadence);
        var sut = new SetReconfirmPolicyHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.ReconfirmPolicies
                .FirstOrDefaultAsync(p => p.Id == fixture.EventId, testContext.CancellationToken);

            policy.ShouldNotBeNull();
            policy.OpensAt.ShouldBe(newOpensAt);
            policy.ClosesAt.ShouldBe(newClosesAt);
            policy.Cadence.ShouldBe(newCadence);
        });
    }

    [TestMethod]
    public async ValueTask SC003_SetReconfirmPolicy_CancelledEvent_ThrowsEventNotActive()
    {
        var fixture = SetReconfirmPolicyFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var opensAt = new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var closesAt = new DateTimeOffset(2025, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var cadence = TimeSpan.FromDays(3);

        var command = new SetReconfirmPolicyCommand(fixture.EventId, opensAt, closesAt, cadence);
        var sut = new SetReconfirmPolicyHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(TicketedEventLifecycleGuard.Errors.EventNotActive);
    }

    [TestMethod]
    public async ValueTask SC004_SetReconfirmPolicy_CloseBeforeOpen_ThrowsWindowCloseBeforeOpen()
    {
        var fixture = SetReconfirmPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var opensAt = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var closesAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero); // before open
        var cadence = TimeSpan.FromDays(3);

        var command = new SetReconfirmPolicyCommand(fixture.EventId, opensAt, closesAt, cadence);
        var sut = new SetReconfirmPolicyHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(Domain.Entities.ReconfirmPolicy.Errors.WindowCloseBeforeOpen);
    }

    [TestMethod]
    public async ValueTask SC005_SetReconfirmPolicy_CadenceBelowMinimum_ThrowsCadenceBelowMinimum()
    {
        var fixture = SetReconfirmPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var opensAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var closesAt = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var cadence = TimeSpan.FromHours(12); // less than 1 day

        var command = new SetReconfirmPolicyCommand(fixture.EventId, opensAt, closesAt, cadence);
        var sut = new SetReconfirmPolicyHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(Domain.Entities.ReconfirmPolicy.Errors.CadenceBelowMinimum);
    }
}
