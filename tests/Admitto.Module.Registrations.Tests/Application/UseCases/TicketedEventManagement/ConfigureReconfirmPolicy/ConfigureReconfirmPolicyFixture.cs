using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.ConfigureReconfirmPolicy;

internal sealed class ConfigureReconfirmPolicyFixture
{
    private bool _cancel;
    private bool _seedExistingPolicy;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public uint SeededVersion { get; private set; }

    private ConfigureReconfirmPolicyFixture() { }

    public static ConfigureReconfirmPolicyFixture ActiveEvent() => new();
    public static ConfigureReconfirmPolicyFixture ActiveWithExistingPolicy() => new() { _seedExistingPolicy = true };
    public static ConfigureReconfirmPolicyFixture CancelledEvent() => new() { _cancel = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        TicketedEvent? seeded = null;

        await environment.Database.SeedAsync(dbContext =>
        {
            var ticketedEvent = TicketedEvent.Create(
                EventId,
                TeamId,
                Slug.From("test-team"),
                Slug.From("reconfirm-policy-event"),
                DisplayName.From("Reconfirm Policy Event"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31),
                TimeZoneId.From("UTC"));

            if (_seedExistingPolicy)
            {
                ticketedEvent.ConfigureReconfirmPolicy(
                    TicketedEventReconfirmPolicy.Create(
                        DateTimeOffset.UtcNow.AddDays(5),
                        DateTimeOffset.UtcNow.AddDays(15),
                        TimeSpan.FromDays(7)));
            }

            if (_cancel) ticketedEvent.Cancel();

            dbContext.TicketedEvents.Add(ticketedEvent);
            seeded = ticketedEvent;
        });

        SeededVersion = seeded!.Version;
    }
}
