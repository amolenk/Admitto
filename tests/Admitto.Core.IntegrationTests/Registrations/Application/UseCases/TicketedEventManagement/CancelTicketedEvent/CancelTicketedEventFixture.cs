using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEventManagement.CancelTicketedEvent;

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
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                EventId,
                TeamId,
                EventName.From("Cancel Event"),
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
