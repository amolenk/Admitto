using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.EventLifecycleSync;

internal sealed class EventLifecycleSyncFixture
{
    private bool _seedPolicy;
    private bool _policyCancelled;

    public TicketedEventId EventId { get; } = TicketedEventId.New();

    private EventLifecycleSyncFixture()
    {
    }

    public static EventLifecycleSyncFixture WithActivePolicy() => new()
    {
        _seedPolicy = true
    };

    public static EventLifecycleSyncFixture WithCancelledPolicy() => new()
    {
        _seedPolicy = true,
        _policyCancelled = true
    };

    public static EventLifecycleSyncFixture NoPolicyExists() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (!_seedPolicy)
            return;

        await environment.Database.SeedAsync(dbContext =>
        {
            var policy = EventRegistrationPolicy.Create(EventId);
            if (_policyCancelled)
            {
                policy.SetCancelled();
            }
            dbContext.EventRegistrationPolicies.Add(policy);
        });
    }
}
