using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.UpdateTicketedEventDetails;

internal sealed class UpdateTicketedEventDetailsFixture
{
    private bool _cancel;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public uint SeededVersion { get; private set; }

    private UpdateTicketedEventDetailsFixture() { }

    public static UpdateTicketedEventDetailsFixture ActiveEvent() => new();
    public static UpdateTicketedEventDetailsFixture CancelledEvent() => new() { _cancel = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        TicketedEvent? seeded = null;

        await environment.Database.SeedAsync(dbContext =>
        {
            var ticketedEvent = TicketedEvent.Create(
                EventId,
                TeamId,
                Slug.From("update-event"),
                DisplayName.From("Original Name"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(1),
                DateTimeOffset.UtcNow.AddDays(2),
                TimeZoneId.From("UTC"));

            if (_cancel) ticketedEvent.Cancel();

            dbContext.TicketedEvents.Add(ticketedEvent);
            seeded = ticketedEvent;
        });

        SeededVersion = seeded!.Version;
    }
}
