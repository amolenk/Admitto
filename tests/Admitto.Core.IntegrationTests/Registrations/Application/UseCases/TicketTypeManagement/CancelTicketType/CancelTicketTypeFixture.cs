using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketTypeManagement.CancelTicketType;

internal sealed class CancelTicketTypeFixture
{
    private bool _ticketTypeAlreadyCancelled;
    private EventLifecycleStatus _eventStatus = EventLifecycleStatus.Active;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketTypeId TicketTypeId { get; } = TicketTypeId.New();

    private CancelTicketTypeFixture()
    {
    }

    public static CancelTicketTypeFixture ActiveEvent() => new();

    public static CancelTicketTypeFixture AlreadyCancelled() => new()
    {
        _ticketTypeAlreadyCancelled = true
    };

    public static CancelTicketTypeFixture CancelledEvent() => new()
    {
        _eventStatus = EventLifecycleStatus.Cancelled
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var catalog = TicketCatalog.Create(EventId);
            catalog.AddTicketType(
                TicketTypeId,
                TicketTypeName.From("General Admission"),
                [],
                100);

            if (_ticketTypeAlreadyCancelled)
            {
                catalog.CancelTicketType(TicketTypeId);
            }

            if (_eventStatus == EventLifecycleStatus.Cancelled)
            {
                catalog.MarkEventCancelled();
            }

            dbContext.TicketCatalogs.Add(catalog);
        });
    }
}
