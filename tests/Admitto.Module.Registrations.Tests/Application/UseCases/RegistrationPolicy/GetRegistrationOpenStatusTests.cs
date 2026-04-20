using Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.GetRegistrationOpenStatus;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.Time.Testing;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.RegistrationPolicy;

[TestClass]
public sealed class GetRegistrationOpenStatusTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC013_GetStatus_NoPolicy_ReportsNotOpen()
    {
        var eventId = TicketedEventId.New();

        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var sut = new GetRegistrationOpenStatusHandler(Environment.Database.Context, timeProvider);

        var result = await sut.HandleAsync(
            new GetRegistrationOpenStatusQuery(eventId), testContext.CancellationToken);

        result.IsOpen.ShouldBeFalse();
        result.IsEventActive.ShouldBeTrue();
        result.WindowOpensAt.ShouldBeNull();
        result.WindowClosesAt.ShouldBeNull();
    }

    [TestMethod]
    public async ValueTask SC014_GetStatus_InsideWindow_ReportsOpen()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;

        await Environment.Database.SeedAsync(dbContext =>
        {
            var policy = EventRegistrationPolicy.Create(eventId);
            policy.SetWindow(now.AddHours(-1), now.AddHours(1));
            dbContext.EventRegistrationPolicies.Add(policy);
        });

        var timeProvider = new FakeTimeProvider(now);
        var sut = new GetRegistrationOpenStatusHandler(Environment.Database.Context, timeProvider);

        var result = await sut.HandleAsync(
            new GetRegistrationOpenStatusQuery(eventId), testContext.CancellationToken);

        result.IsOpen.ShouldBeTrue();
        result.IsEventActive.ShouldBeTrue();
    }

    [TestMethod]
    public async ValueTask SC015_GetStatus_EventCancelled_ReportsNotOpen()
    {
        var eventId = TicketedEventId.New();
        var now = DateTimeOffset.UtcNow;

        await Environment.Database.SeedAsync(dbContext =>
        {
            var guard = TicketedEventLifecycleGuard.Create(eventId);
            guard.SetCancelled();
            dbContext.TicketedEventLifecycleGuards.Add(guard);

            var policy = EventRegistrationPolicy.Create(eventId);
            policy.SetWindow(now.AddHours(-1), now.AddHours(1));
            dbContext.EventRegistrationPolicies.Add(policy);
        });

        var timeProvider = new FakeTimeProvider(now);
        var sut = new GetRegistrationOpenStatusHandler(Environment.Database.Context, timeProvider);

        var result = await sut.HandleAsync(
            new GetRegistrationOpenStatusQuery(eventId), testContext.CancellationToken);

        result.IsOpen.ShouldBeFalse();
        result.IsEventActive.ShouldBeFalse();
    }
}
