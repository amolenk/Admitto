using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Coupons.CreateCoupon;

internal sealed class CreateCouponFixture
{
    public TeamId TeamId { get; } = TeamId.New();
    public TicketedEventId EventId { get; } = TicketedEventId.New();
    public TicketTypeId TicketTypeId { get; } = TicketTypeId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    private CreateCouponFixture()
    {
    }

    public static CreateCouponFixture HappyFlow() => new();

    public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
    {
        var catalog = TicketCatalog.Create(EventId, TeamId);
        catalog.AddTicketType(
            TicketTypeId, TicketTypeName.From("General Admission"), [], 100);

        await environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            dbContext.TicketCatalogs.Add(catalog);
        });
    }
}
