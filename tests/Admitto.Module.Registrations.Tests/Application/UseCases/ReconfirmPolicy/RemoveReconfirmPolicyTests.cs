using Amolenk.Admitto.Module.Registrations.Application.UseCases.ReconfirmPolicy.RemoveReconfirmPolicy;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.ReconfirmPolicy;

[TestClass]
public sealed class RemoveReconfirmPolicyTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC001_RemoveReconfirmPolicy_ExistingPolicy_DeletesPolicy()
    {
        var eventId = TicketedEventId.New();

        await Environment.Database.SeedAsync(dbContext =>
        {
            var guard = TicketedEventLifecycleGuard.Create(eventId);
            dbContext.TicketedEventLifecycleGuards.Add(guard);

            var policy = Domain.Entities.ReconfirmPolicy.Create(
                eventId,
                new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero),
                TimeSpan.FromDays(7));
            dbContext.ReconfirmPolicies.Add(policy);
        });

        var command = new RemoveReconfirmPolicyCommand(eventId);
        var sut = new RemoveReconfirmPolicyHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.ReconfirmPolicies
                .FirstOrDefaultAsync(p => p.Id == eventId, testContext.CancellationToken);

            policy.ShouldBeNull();
        });
    }

    [TestMethod]
    public async ValueTask SC002_RemoveReconfirmPolicy_NoExistingPolicy_NoOp()
    {
        var eventId = TicketedEventId.New();

        await Environment.Database.SeedAsync(dbContext =>
        {
            var guard = TicketedEventLifecycleGuard.Create(eventId);
            dbContext.TicketedEventLifecycleGuards.Add(guard);
        });

        var command = new RemoveReconfirmPolicyCommand(eventId);
        var sut = new RemoveReconfirmPolicyHandler(Environment.Database.Context);

        // Should not throw
        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.ReconfirmPolicies
                .FirstOrDefaultAsync(p => p.Id == eventId, testContext.CancellationToken);

            policy.ShouldBeNull();
        });
    }

    [TestMethod]
    public async ValueTask SC003_RemoveReconfirmPolicy_CancelledEvent_ThrowsEventNotActive()
    {
        var eventId = TicketedEventId.New();

        await Environment.Database.SeedAsync(dbContext =>
        {
            var guard = TicketedEventLifecycleGuard.Create(eventId);
            guard.SetCancelled();
            dbContext.TicketedEventLifecycleGuards.Add(guard);
        });

        var command = new RemoveReconfirmPolicyCommand(eventId);
        var sut = new RemoveReconfirmPolicyHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(TicketedEventLifecycleGuard.Errors.EventNotActive);
    }
}
