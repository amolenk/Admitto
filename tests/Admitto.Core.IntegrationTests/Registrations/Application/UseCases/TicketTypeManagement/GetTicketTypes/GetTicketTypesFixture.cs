using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketTypeManagement.GetTicketTypes;

internal sealed class GetTicketTypesFixture
{
    private bool _seedCatalog;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketTypeId GeneralAdmissionId { get; } = TicketTypeId.New();
    public TicketTypeId VipPassId { get; } = TicketTypeId.New();

    private GetTicketTypesFixture()
    {
    }

    public static GetTicketTypesFixture WithMixedTicketTypes() => new()
    {
        _seedCatalog = true
    };

    public static GetTicketTypesFixture NoCatalog() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (!_seedCatalog)
            return;

        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var catalog = TicketCatalog.Create(EventId);
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
            catalog.CancelTicketType(VipPassId);

            dbContext.TicketCatalogs.Add(catalog);
        });
    }
}
