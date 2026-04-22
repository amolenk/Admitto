using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Infrastructure.Hosting;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.CouponManagement.CreateCoupon;

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
            Slug.From(TicketTypeSlug), DisplayName.From("General Admission"), [], 100);

        if (_hasCancelledTicketType)
        {
            catalog.AddTicketType(
                Slug.From(CancelledTicketTypeSlug), DisplayName.From("VIP Pass"), [], 50);
            catalog.CancelTicketType(Slug.From(CancelledTicketTypeSlug));
        }

        await environment.Database.SeedAsync(dbContext =>
        {
            dbContext.TicketCatalogs.Add(catalog);
        });
    }
}
