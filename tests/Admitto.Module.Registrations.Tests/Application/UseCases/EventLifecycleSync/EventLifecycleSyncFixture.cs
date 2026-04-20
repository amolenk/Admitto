using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.EventLifecycleSync;

internal sealed class EventLifecycleSyncFixture
{
    private bool _seedGuard;
    private bool _guardCancelled;

    public TicketedEventId EventId { get; } = TicketedEventId.New();

    private EventLifecycleSyncFixture()
    {
    }

    public static EventLifecycleSyncFixture WithActiveGuard() => new()
    {
        _seedGuard = true
    };

    public static EventLifecycleSyncFixture WithCancelledGuard() => new()
    {
        _seedGuard = true,
        _guardCancelled = true
    };

    public static EventLifecycleSyncFixture NoGuardExists() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (!_seedGuard)
            return;

        await environment.Database.SeedAsync(dbContext =>
        {
            var guard = TicketedEventLifecycleGuard.Create(EventId);
            if (_guardCancelled)
            {
                guard.SetCancelled();
            }
            dbContext.TicketedEventLifecycleGuards.Add(guard);
        });
    }
}
