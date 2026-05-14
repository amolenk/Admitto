using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.CouponManagement.CreateCoupon;

internal sealed class CreateCouponFixture
{
    private bool _hasCancelledTicketType;

    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public string TicketTypeSlug { get; } = "general-admission";
    public string CancelledTicketTypeSlug { get; } = "vip-pass";

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
            Slug.From(TicketTypeSlug), TicketTypeName.From("General Admission"), [], 100);

        if (_hasCancelledTicketType)
        {
            catalog.AddTicketType(
                Slug.From(CancelledTicketTypeSlug), TicketTypeName.From("VIP Pass"), [], 50);
            catalog.CancelTicketType(Slug.From(CancelledTicketTypeSlug));
        }

        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            dbContext.TicketCatalogs.Add(catalog);
        });
    }
}
