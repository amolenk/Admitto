using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Infrastructure.Persistence;

[TestClass]
public sealed class TicketCatalogPersistenceTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a ticket type with seats held by outstanding waitlist offers
    // When the catalog is saved and loaded again
    // Then the held capacity round-trips
    [TestMethod]
    public async ValueTask Load_WaitlistHeldCapacity_RoundTrips()
    {
        // Arrange
        var (catalog, ticketTypeId) = CreateCatalog();
        catalog.HoldForWaitlistOffer(ticketTypeId);
        catalog.HoldForWaitlistOffer(ticketTypeId);

        await Environment.RegistrationsDatabase.SeedAsync(
            dbContext => dbContext.TicketCatalogs.Add(catalog), testContext.CancellationToken);

        // Act & Assert
        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var loaded = await ctx.TicketCatalogs.SingleAsync(c => c.Id == catalog.Id, testContext.CancellationToken);
            loaded.FindTicketType(ticketTypeId).WaitlistHeldCapacity.ShouldBe(2);
        });
    }

    // Given a stored ticket type written before waitlist offers held capacity
    // When the catalog is loaded
    // Then its held capacity reads as zero
    [TestMethod]
    public async ValueTask Load_TicketTypeJsonWithoutWaitlistHeldCapacity_ReadsAsZero()
    {
        // Arrange — persist a hold, then strip the key from the stored JSON
        var (catalog, ticketTypeId) = CreateCatalog();
        catalog.HoldForWaitlistOffer(ticketTypeId);

        await Environment.RegistrationsDatabase.SeedAsync(
            dbContext => dbContext.TicketCatalogs.Add(catalog), testContext.CancellationToken);

        await Environment.RegistrationsDatabase.Context.Database.ExecuteSqlAsync(
            $"""
             UPDATE registrations.ticket_catalog
             SET ticket_types = (SELECT jsonb_agg(t - 'waitlist_held_capacity') FROM jsonb_array_elements(ticket_types) t)
             WHERE event_id = {catalog.Id.Value}
             """,
            testContext.CancellationToken);
        Environment.RegistrationsDatabase.Context.ChangeTracker.Clear();

        // Act & Assert
        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var loaded = await ctx.TicketCatalogs.SingleAsync(c => c.Id == catalog.Id, testContext.CancellationToken);
            loaded.FindTicketType(ticketTypeId).WaitlistHeldCapacity.ShouldBe(0);
        });
    }

    private static (TicketCatalog Catalog, TicketTypeId TicketTypeId) CreateCatalog()
    {
        var ticketTypeId = TicketTypeId.New();
        var catalog = TicketCatalog.Create(TicketedEventId.New(), TeamId.New());
        catalog.AddTicketType(ticketTypeId, TicketTypeName.From("Conference Pass"), [], maxCapacity: 10,
            waitlistEnabled: true);
        return (catalog, ticketTypeId);
    }
}
