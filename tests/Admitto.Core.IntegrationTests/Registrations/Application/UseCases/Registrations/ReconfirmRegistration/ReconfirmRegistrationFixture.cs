using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.ReconfirmRegistration;

internal sealed class ReconfirmRegistrationFixture
{
    private bool _preCancel;
    private bool _archiveEvent;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();

    private ReconfirmRegistrationFixture()
    {
    }

    public static ReconfirmRegistrationFixture ActiveRegistration() => new();

    public static ReconfirmRegistrationFixture AlreadyCancelled() => new() { _preCancel = true };

    public static ReconfirmRegistrationFixture WithArchivedEvent() => new() { _archiveEvent = true };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var ticketTypeId = TicketTypeId.New();

            var catalog = TicketCatalog.Create(EventId, TeamId);
            catalog.AddTicketType(ticketTypeId, TicketTypeName.From("General Admission"), [], 100);

            if (_archiveEvent)
            {
                catalog.MarkEventArchived();
            }

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
        });
    }
}
