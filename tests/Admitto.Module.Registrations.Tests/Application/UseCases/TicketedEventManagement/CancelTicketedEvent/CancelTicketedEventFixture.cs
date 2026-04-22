using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.CancelTicketedEvent;

internal sealed class CancelTicketedEventFixture
{
    private bool _preCancel;
    private bool _preArchive;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();

    private CancelTicketedEventFixture() { }

    public static CancelTicketedEventFixture ActiveEvent() => new();
    public static CancelTicketedEventFixture AlreadyCancelled() => new() { _preCancel = true };
    public static CancelTicketedEventFixture AlreadyArchived() => new() { _preArchive = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.Database.SeedAsync(dbContext =>
        {
            var ticketedEvent = TicketedEvent.Create(
                EventId,
                TeamId,
                Slug.From("cancel-event"),
                DisplayName.From("Cancel Event"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(1),
                DateTimeOffset.UtcNow.AddDays(2));

            if (_preCancel) ticketedEvent.Cancel();
            if (_preArchive) ticketedEvent.Archive();

            dbContext.TicketedEvents.Add(ticketedEvent);
        });
    }
}
