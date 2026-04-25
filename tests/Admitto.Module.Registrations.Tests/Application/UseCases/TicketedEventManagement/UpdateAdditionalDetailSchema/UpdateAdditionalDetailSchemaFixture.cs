using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.UpdateAdditionalDetailSchema;

internal sealed class UpdateAdditionalDetailSchemaFixture
{
    private bool _cancel;
    private bool _archive;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public uint SeededVersion { get; private set; }

    private UpdateAdditionalDetailSchemaFixture() { }

    public static UpdateAdditionalDetailSchemaFixture ActiveEvent() => new();
    public static UpdateAdditionalDetailSchemaFixture CancelledEvent() => new() { _cancel = true };
    public static UpdateAdditionalDetailSchemaFixture ArchivedEvent() => new() { _archive = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        TicketedEvent? seeded = null;

        await environment.Database.SeedAsync(dbContext =>
        {
            var ticketedEvent = TicketedEvent.Create(
                EventId,
                TeamId,
                Slug.From("add-details-event"),
                DisplayName.From("Add Details Event"),
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
