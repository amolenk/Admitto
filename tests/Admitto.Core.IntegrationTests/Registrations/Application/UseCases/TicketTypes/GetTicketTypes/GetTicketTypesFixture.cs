using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketTypes.GetTicketTypes;

internal sealed class GetTicketTypesFixture
{
    private bool _seedCatalog;
    private (int maxCapacity, int reservedCapacity, int reservedUsed)? _reservedCapacityScenario;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TeamId TeamId { get; } = TeamId.New();
    public TicketTypeId GeneralAdmissionId { get; } = TicketTypeId.New();
    public TicketTypeId VipPassId { get; } = TicketTypeId.New();
    public TicketTypeId WorkshopId { get; } = TicketTypeId.New();

    private GetTicketTypesFixture()
    {
    }

    public static GetTicketTypesFixture WithMixedTicketTypes() => new()
    {
        _seedCatalog = true
    };

    public static GetTicketTypesFixture WithReservedCapacityPartlyUsed(
        int maxCapacity, int reservedCapacity, int reservedUsed) => new()
    {
        _reservedCapacityScenario = (maxCapacity, reservedCapacity, reservedUsed)
    };

    public static GetTicketTypesFixture NoCatalog() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (_reservedCapacityScenario is { } scenario)
        {
            await environment.RegistrationsDatabase.SeedAsync(dbContext =>
            {
                var catalog = TicketCatalog.Create(EventId, TeamId);
                catalog.AddTicketType(
                    WorkshopId,
                    TicketTypeName.From("Workshop"),
                    [],
                    scenario.maxCapacity,
                    reservedCapacity: scenario.reservedCapacity);

                for (var i = 0; i < scenario.reservedUsed; i++)
                    catalog.Claim([WorkshopId], ClaimMode.Reserved);

                dbContext.TicketCatalogs.Add(catalog);
            });
            return;
        }

        if (!_seedCatalog)
            return;

        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var catalog = TicketCatalog.Create(EventId, TeamId);
            catalog.AddTicketType(
                GeneralAdmissionId,
                TicketTypeName.From("General Admission"),
                [TimeSlot.From("morning")],
                100);
            catalog.AddTicketType(
                VipPassId,
                TicketTypeName.From("VIP Pass"),
                [],
                50);

            dbContext.TicketCatalogs.Add(catalog);
        });
    }
}
