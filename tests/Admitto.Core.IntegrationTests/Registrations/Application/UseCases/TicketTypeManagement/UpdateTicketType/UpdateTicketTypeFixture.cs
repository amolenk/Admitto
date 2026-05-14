using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType;

internal sealed class UpdateTicketTypeFixture
{
    private EventLifecycleStatus _eventStatus = EventLifecycleStatus.Active;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public string TicketTypeSlug { get; } = "general-admission";

    private UpdateTicketTypeFixture()
    {
    }

    public static UpdateTicketTypeFixture ActiveEvent() => new();

    public static UpdateTicketTypeFixture CancelledEvent() => new()
    {
        _eventStatus = EventLifecycleStatus.Cancelled
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var catalog = TicketCatalog.Create(EventId);
            catalog.AddTicketType(
                Slug.From(TicketTypeSlug),
                TicketTypeName.From("General Admission"),
                [],
                100);

            if (_eventStatus == EventLifecycleStatus.Cancelled)
            {
                catalog.MarkEventCancelled();
            }

            dbContext.TicketCatalogs.Add(catalog);
        });
    }
}
