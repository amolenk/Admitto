using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.CancelRegistration;

internal sealed class CancelRegistrationFixture
{
    private bool _preCancel;
    private DateTimeOffset? _eventStartsAt;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();

    private CancelRegistrationFixture()
    {
    }

    public static CancelRegistrationFixture ActiveRegistration() => new();

    public static CancelRegistrationFixture AlreadyCancelled() => new() { _preCancel = true };

    public static CancelRegistrationFixture WithEventAlreadyStarted() =>
        new() { _eventStartsAt = DateTimeOffset.UtcNow.AddDays(-1) };

    public static CancelRegistrationFixture WithEventNotYetStarted() =>
        new() { _eventStartsAt = DateTimeOffset.UtcNow.AddDays(60) };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var ticketTypeId = TicketTypeId.New();

            var catalog = TicketCatalog.Create(EventId, TeamId);
            catalog.AddTicketType(ticketTypeId, TicketTypeName.From("General Admission"), [], 100);
            dbContext.TicketCatalogs.Add(catalog);

            var registration = Registration.Create(
                TeamId,
                EventId,
                EmailAddress.From("alice@example.com"),
                FirstName.From("Alice"),
                LastName.From("Test"),
                [new TicketTypeSnapshot(ticketTypeId, TicketTypeName.From("General Admission"), [])]);

            RegistrationId = registration.Id;

            if (_preCancel)
            {
                registration.Cancel(CancellationReason.AttendeeRequest);
            }

            dbContext.Registrations.Add(registration);

            if (_eventStartsAt.HasValue)
            {
                var ticketedEvent = TicketedEvent.Create(
                    CreationRequestId.From(Guid.NewGuid()),
                    EventId,
                    TeamId,
                    EventName.From("DevConf"),
                    AbsoluteUrl.From("https://example.com"),
                    AbsoluteUrl.From("https://tickets.example.com"),
                    _eventStartsAt.Value,
                    _eventStartsAt.Value.AddDays(1),
                    TimeZoneId.From("UTC"));
                dbContext.TicketedEvents.Add(ticketedEvent);
            }
        });
    }
}
