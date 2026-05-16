using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEventManagement.ConfigureReconfirmPolicy;

internal sealed class ConfigureReconfirmPolicyFixture
{
    private bool _seedExistingPolicy;
    private bool _archive;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public uint SeededVersion { get; private set; }

    private ConfigureReconfirmPolicyFixture() { }

    public static ConfigureReconfirmPolicyFixture ActiveEvent() => new();
    public static ConfigureReconfirmPolicyFixture ActiveWithExistingPolicy() => new() { _seedExistingPolicy = true };
    public static ConfigureReconfirmPolicyFixture ArchivedEvent() => new() { _archive = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        TicketedEvent? seeded = null;

        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                EventId,
                TeamId,
                EventName.From("Reconfirm Policy Event"),
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
                        TimeSpan.FromDays(7),
                        TimeSpan.FromHours(24)));
            }

            if (_archive) ticketedEvent.Archive();

            dbContext.TicketedEvents.Add(ticketedEvent);
            seeded = ticketedEvent;
        });

        SeededVersion = seeded!.Version;
    }
}
