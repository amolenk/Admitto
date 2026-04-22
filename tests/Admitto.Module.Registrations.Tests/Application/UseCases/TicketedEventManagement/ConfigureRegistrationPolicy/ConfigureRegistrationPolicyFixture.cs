using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.ConfigureRegistrationPolicy;

internal sealed class ConfigureRegistrationPolicyFixture
{
    private bool _cancel;
    private bool _archive;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public uint SeededVersion { get; private set; }

    private ConfigureRegistrationPolicyFixture() { }

    public static ConfigureRegistrationPolicyFixture ActiveEvent() => new();
    public static ConfigureRegistrationPolicyFixture CancelledEvent() => new() { _cancel = true };
    public static ConfigureRegistrationPolicyFixture ArchivedEvent() => new() { _archive = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        TicketedEvent? seeded = null;

        await environment.Database.SeedAsync(dbContext =>
        {
            var ticketedEvent = TicketedEvent.Create(
                EventId,
                TeamId,
                Slug.From("reg-policy-event"),
                DisplayName.From("Reg Policy Event"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31));

            if (_cancel) ticketedEvent.Cancel();
            if (_archive) ticketedEvent.Archive();

            dbContext.TicketedEvents.Add(ticketedEvent);
            seeded = ticketedEvent;
        });

        SeededVersion = seeded!.Version;
    }
}
