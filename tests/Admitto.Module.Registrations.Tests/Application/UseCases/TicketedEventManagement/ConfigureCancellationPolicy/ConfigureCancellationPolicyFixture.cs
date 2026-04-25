using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.ConfigureCancellationPolicy;

internal sealed class ConfigureCancellationPolicyFixture
{
    private bool _cancel;
    private bool _seedExistingPolicy;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public uint SeededVersion { get; private set; }

    private ConfigureCancellationPolicyFixture() { }

    public static ConfigureCancellationPolicyFixture ActiveEvent() => new();
    public static ConfigureCancellationPolicyFixture ActiveWithExistingPolicy() => new() { _seedExistingPolicy = true };
    public static ConfigureCancellationPolicyFixture CancelledEvent() => new() { _cancel = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        TicketedEvent? seeded = null;

        await environment.Database.SeedAsync(dbContext =>
        {
            var ticketedEvent = TicketedEvent.Create(
                EventId,
                TeamId,
                Slug.From("cancel-policy-event"),
                DisplayName.From("Cancel Policy Event"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31),
                TimeZoneId.From("UTC"));

            if (_seedExistingPolicy)
            {
                ticketedEvent.ConfigureCancellationPolicy(
                    new TicketedEventCancellationPolicy(DateTimeOffset.UtcNow.AddDays(20)));
            }

            if (_cancel) ticketedEvent.Cancel();

            dbContext.TicketedEvents.Add(ticketedEvent);
            seeded = ticketedEvent;
        });

        SeededVersion = seeded!.Version;
    }
}
