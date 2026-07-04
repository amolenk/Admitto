using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketTypes.UpdateTicketType;

internal sealed class UpdateTicketTypeFixture
{
    private EventLifecycleStatus _eventStatus = EventLifecycleStatus.Active;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public TicketTypeId TicketTypeId { get; } = TicketTypeId.New();

    private UpdateTicketTypeFixture()
    {
    }

    public static UpdateTicketTypeFixture ActiveEvent() => new();

    public static UpdateTicketTypeFixture ArchivedEvent() => new()
    {
        _eventStatus = EventLifecycleStatus.Archived
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var catalog = TicketCatalog.Create(EventId, TeamId);
            catalog.AddTicketType(
                TicketTypeId,
                TicketTypeName.From("General Admission"),
                [],
                100);

            if (_eventStatus == EventLifecycleStatus.Archived)
            {
                catalog.MarkEventArchived();
            }

            dbContext.TicketCatalogs.Add(catalog);
        });
    }
}
