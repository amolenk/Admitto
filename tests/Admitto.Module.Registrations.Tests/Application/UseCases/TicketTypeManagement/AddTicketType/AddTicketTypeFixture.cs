using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketTypeManagement.AddTicketType;

internal sealed class AddTicketTypeFixture
{
    private bool _seedExistingTicketType;
    private EventLifecycleStatus _eventStatus = EventLifecycleStatus.Active;

    public TicketedEventId EventId { get; } = TicketedEventId.New();

    private AddTicketTypeFixture()
    {
    }

    public static AddTicketTypeFixture ActiveEvent() => new();

    public static AddTicketTypeFixture ActiveEventWithCatalog() => new()
    {
        _seedExistingTicketType = true
    };

    public static AddTicketTypeFixture CancelledEvent() => new()
    {
        _eventStatus = EventLifecycleStatus.Cancelled
    };

    public static AddTicketTypeFixture ArchivedEvent() => new()
    {
        _eventStatus = EventLifecycleStatus.Archived
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.Database.SeedAsync(dbContext =>
        {
            var catalog = TicketCatalog.Create(EventId);

            if (_seedExistingTicketType)
            {
                catalog.AddTicketType(
                    Slug.From("existing-type"),
                    DisplayName.From("Existing Type"),
                    [],
                    100);
            }

            if (_eventStatus == EventLifecycleStatus.Cancelled)
            {
                catalog.MarkEventCancelled();
            }
            else if (_eventStatus == EventLifecycleStatus.Archived)
            {
                catalog.MarkEventArchived();
            }

            dbContext.TicketCatalogs.Add(catalog);
        });
    }
}
