using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEventManagement.ConfigureRegistrationPolicy;

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

        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                EventId,
                TeamId,
                EventName.From("Reg Policy Event"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31),
                TimeZoneId.From("UTC"));

            if (_cancel) ticketedEvent.Cancel();
            if (_archive) ticketedEvent.Archive();

            dbContext.TicketedEvents.Add(ticketedEvent);
            seeded = ticketedEvent;
        });

        SeededVersion = seeded!.Version;
    }
}
