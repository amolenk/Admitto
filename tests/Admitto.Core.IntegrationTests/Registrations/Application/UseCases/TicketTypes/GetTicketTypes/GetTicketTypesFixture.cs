using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketTypes.GetTicketTypes;

internal sealed class GetTicketTypesFixture
{
    private bool _seedCatalog;
    private (int publicCapacity, int publicUsed, int adminUsed)? _publicAndAdminScenario;
    private (int queued, int held)? _waitlistScenario;

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

    public static GetTicketTypesFixture WithPublicAndAdminTickets(
        int publicCapacity, int publicUsed, int adminUsed) => new()
    {
        _publicAndAdminScenario = (publicCapacity, publicUsed, adminUsed)
    };

    public static GetTicketTypesFixture WithWaitlist(int queued, int held) => new()
    {
        _waitlistScenario = (queued, held)
    };

    public static GetTicketTypesFixture NoCatalog() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        if (_publicAndAdminScenario is { } scenario)
        {
            await environment.RegistrationsDatabase.SeedAsync(dbContext =>
            {
                var catalog = TicketCatalog.Create(EventId, TeamId);
                catalog.AddTicketType(
                    WorkshopId,
                    TicketTypeName.From("Workshop"),
                    [],
                    scenario.publicCapacity);

                for (var i = 0; i < scenario.publicUsed; i++)
                    catalog.Claim([WorkshopId], ClaimMode.Public);
                for (var i = 0; i < scenario.adminUsed; i++)
                    catalog.Claim([WorkshopId], ClaimMode.Admin);

                dbContext.TicketCatalogs.Add(catalog);
            });
            return;
        }

        if (_waitlistScenario is { } waitlist)
        {
            await environment.RegistrationsDatabase.SeedAsync(dbContext =>
            {
                var catalog = TicketCatalog.Create(EventId, TeamId);
                catalog.AddTicketType(
                    WorkshopId,
                    TicketTypeName.From("Workshop"),
                    [],
                    publicCapacity: 10);

                for (var i = 0; i < waitlist.queued; i++)
                    catalog.JoinWaitlistQueue(WorkshopId);
                for (var i = 0; i < waitlist.held; i++)
                    catalog.HoldForWaitlistOffer(WorkshopId);

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
