using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.ReconfirmPolicy;

internal sealed class SetReconfirmPolicyFixture
{
    private bool _eventCancelled;
    private bool _hasExistingPolicy;

    public TicketedEventId EventId { get; } = TicketedEventId.New();

    public DateTimeOffset ExistingOpensAt { get; } = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public DateTimeOffset ExistingClosesAt { get; } = new(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
    public TimeSpan ExistingCadence { get; } = TimeSpan.FromDays(7);

    private SetReconfirmPolicyFixture() { }

    public static SetReconfirmPolicyFixture ActiveEvent() => new();

    public static SetReconfirmPolicyFixture ActiveEventWithExistingPolicy() => new()
    {
        _hasExistingPolicy = true
    };

    public static SetReconfirmPolicyFixture CancelledEvent() => new()
    {
        _eventCancelled = true
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.Database.SeedAsync(dbContext =>
        {
            var guard = TicketedEventLifecycleGuard.Create(EventId);
            if (_eventCancelled) guard.SetCancelled();
            dbContext.TicketedEventLifecycleGuards.Add(guard);

            if (_hasExistingPolicy)
            {
                var policy = Domain.Entities.ReconfirmPolicy.Create(
                    EventId, ExistingOpensAt, ExistingClosesAt, ExistingCadence);
                dbContext.ReconfirmPolicies.Add(policy);
            }
        });
    }
}
