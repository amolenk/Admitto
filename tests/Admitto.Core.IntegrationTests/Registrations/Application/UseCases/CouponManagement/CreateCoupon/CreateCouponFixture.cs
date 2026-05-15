using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.CouponManagement.CreateCoupon;

internal sealed class CreateCouponFixture
{
    private bool _hasCancelledTicketType;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketTypeId TicketTypeId { get; } = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    public TicketTypeId CancelledTicketTypeId { get; } = TicketTypeId.From(new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));

    private CreateCouponFixture()
    {
    }

    public static CreateCouponFixture HappyFlow() => new();

    public static CreateCouponFixture WithCancelledTicketType() => new()
    {
        _hasCancelledTicketType = true
    };

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var catalog = TicketCatalog.Create(EventId);
        catalog.AddTicketType(
            TicketTypeId, TicketTypeName.From("General Admission"), [], 100);

        if (_hasCancelledTicketType)
        {
            catalog.AddTicketType(
                CancelledTicketTypeId, TicketTypeName.From("VIP Pass"), [], 50);
            catalog.CancelTicketType(CancelledTicketTypeId);
        }

        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            dbContext.TicketCatalogs.Add(catalog);
        });
    }
}
