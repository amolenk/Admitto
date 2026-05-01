using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.ArchiveTicketedEvent;

internal sealed class ArchiveTicketedEventFixture
{
    private bool _preCancel;
    private bool _preArchive;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();

    private ArchiveTicketedEventFixture() { }

    public static ArchiveTicketedEventFixture ActiveEvent() => new();
    public static ArchiveTicketedEventFixture CancelledEvent() => new() { _preCancel = true };
    public static ArchiveTicketedEventFixture AlreadyArchived() => new() { _preArchive = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.Database.SeedAsync(dbContext =>
        {
            var ticketedEvent = TicketedEvent.Create(
                EventId,
                TeamId,
                Slug.From("test-team"),
                Slug.From("archive-event"),
                DisplayName.From("Archive Event"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(1),
                DateTimeOffset.UtcNow.AddDays(2),
                TimeZoneId.From("UTC"));

            if (_preCancel) ticketedEvent.Cancel();
            if (_preArchive) ticketedEvent.Archive();

            dbContext.TicketedEvents.Add(ticketedEvent);
        });
    }
}
