using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketTypeManagement.GetTicketTypes;

internal sealed class GetTicketTypesFixture
{
    private bool _seedCatalog;

    public TicketedEventId EventId { get; } = TicketedEventId.New();

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

        await environment.Database.SeedAsync(dbContext =>
        {
            var catalog = TicketCatalog.Create(EventId);
            catalog.AddTicketType(
                Slug.From("general-admission"),
                DisplayName.From("General Admission"),
                [new TimeSlot(Slug.From("morning"))],
                100);
            catalog.AddTicketType(
                Slug.From("vip-pass"),
                DisplayName.From("VIP Pass"),
                [],
                50);
            catalog.CancelTicketType(Slug.From("vip-pass"));

            dbContext.TicketCatalogs.Add(catalog);
        });
    }
}
