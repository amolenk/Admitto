using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.ArchiveTicketedEvent;

internal sealed class ArchiveTicketedEventFixture
{
    private bool _preArchive;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();

    private ArchiveTicketedEventFixture() { }

    public static ArchiveTicketedEventFixture ActiveEvent() => new();
    public static ArchiveTicketedEventFixture AlreadyArchived() => new() { _preArchive = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                EventId,
                TeamId,
                EventName.From("Archive Event"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(1),
                DateTimeOffset.UtcNow.AddDays(2),
                TimeZoneId.From("UTC"));

            if (_preArchive) ticketedEvent.Archive();

            dbContext.TicketedEvents.Add(ticketedEvent);
        });
    }
}
