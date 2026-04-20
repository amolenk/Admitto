using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

using CancellationPolicyEntity = Amolenk.Admitto.Module.Registrations.Domain.Entities.CancellationPolicy;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.CancellationPolicy;

internal sealed class SetCancellationPolicyFixture
{
    private bool _eventCancelled;
    private bool _policyExists;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public DateTimeOffset ExistingCutoff { get; } = new(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private SetCancellationPolicyFixture()
    {
    }

    public static SetCancellationPolicyFixture ActiveEvent() => new();

    public static SetCancellationPolicyFixture ActiveEventWithPolicy() => new()
    {
        _policyExists = true
    };

    public static SetCancellationPolicyFixture CancelledEvent() => new()
    {
        _eventCancelled = true
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.Database.SeedAsync(dbContext =>
        {
            var guard = TicketedEventLifecycleGuard.Create(EventId);
            if (_eventCancelled)
            {
                guard.SetCancelled();
            }

            dbContext.TicketedEventLifecycleGuards.Add(guard);

            if (_policyExists)
            {
                var policy = CancellationPolicyEntity.Create(EventId, ExistingCutoff);
                dbContext.CancellationPolicies.Add(policy);
            }
        });
    }
}
