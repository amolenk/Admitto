using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.HandleReconfirmAutoExpired;

internal sealed class HandleReconfirmAutoExpiredFixture
{
    private bool _cancelled;
    private bool _archived;
    private bool _reconfirmed;

    public TeamId TeamId { get; } = TeamId.New();
    public TicketedEventId TicketedEventId { get; } = TicketedEventId.New();
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();

    private HandleReconfirmAutoExpiredFixture() { }

    public static HandleReconfirmAutoExpiredFixture ActiveRegistration() => new();
    public static HandleReconfirmAutoExpiredFixture ArchivedEventRegistration() => new() { _archived = true };
    public static HandleReconfirmAutoExpiredFixture CancelledRegistration() => new() { _cancelled = true };
    public static HandleReconfirmAutoExpiredFixture ReconfirmedRegistration() => new() { _reconfirmed = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        Registration? seeded = null;

        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var ticketTypeId = TicketTypeId.New();
            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                TicketedEventId,
                TeamId,
                EventName.From("DevConf"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31),
                TimeZoneId.From("UTC"));
            dbContext.TicketedEvents.Add(ticketedEvent);

            var catalog = TicketCatalog.Create(TicketedEventId, TeamId);
            catalog.AddTicketType(ticketTypeId, TicketTypeName.From("General"), [], 100);

            if (_archived)
            {
                ticketedEvent.Archive();
                catalog.MarkEventArchived();
            }

            dbContext.TicketCatalogs.Add(catalog);

            var registration = Registration.Create(
                TeamId,
                TicketedEventId,
                EmailAddress.From("alice@example.com"),
                FirstName.From("Alice"),
                LastName.From("Test"),
                [new TicketTypeSnapshot(ticketTypeId, TicketTypeName.From("General"), [])]);

            if (_reconfirmed)
            {
                registration.Reconfirm(DateTimeOffset.UtcNow);
            }

            if (_cancelled)
            {
                registration.Cancel(CancellationReason.AttendeeRequest);
            }

            dbContext.Registrations.Add(registration);
            seeded = registration;
        });

        RegistrationId = seeded!.Id;
    }
}
