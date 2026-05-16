using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType;

internal sealed class AddTicketTypeFixture
{
    private bool _seedExistingTicketType;
    private bool _archive;

    public TicketedEventId EventId { get; } = TicketedEventId.New();

    private AddTicketTypeFixture()
    {
    }

    public static AddTicketTypeFixture ActiveEvent() => new();

    public static AddTicketTypeFixture ActiveEventWithCatalog() => new()
    {
        _seedExistingTicketType = true
    };

    public static AddTicketTypeFixture ArchivedEvent() => new()
    {
        _archive = true
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var catalog = TicketCatalog.Create(EventId);

            if (_seedExistingTicketType)
            {
                catalog.AddTicketType(
                    TicketTypeId.New(),
                    TicketTypeName.From("Existing Type"),
                    [],
                    100);
            }

            if (_archive)
            {
                catalog.MarkEventArchived();
            }

            dbContext.TicketCatalogs.Add(catalog);
        });
    }
}
