using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ReleaseTickets;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.ReleaseTickets;

[TestClass]
public sealed class ReleaseTicketsTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a registration with tickets from a catalog that has used capacity
    // When the tickets are released
    // Then the used capacity of the matching ticket types is decremented
    // Cancelling a registration decrements UsedCapacity on matching ticket types
    [TestMethod]
    public async ValueTask ReleaseTickets_WithMatchingCatalog_DecrementsUsedCapacity()
    {
        var fixture = ReleaseTicketsFixture.WithCatalogAndRegistration(maxCapacity: 10, usedCapacity: 3);
        await fixture.SetupAsync(Environment);

        var command = new ReleaseTicketsCommand(fixture.RegistrationId.Value, fixture.EventId.Value, fixture.TeamId.Value);
        var sut = new ReleaseTicketsHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.GetTicketType(fixture.TicketTypeId)!.UsedCapacity.ShouldBe(2);
        });
    }

    // Given a coupon-only registration with no ticket catalog
    // When the tickets are released
    // Then the operation completes without error
    // Release is skipped when no ticket catalog exists (coupon-only registration)
    [TestMethod]
    public async ValueTask ReleaseTickets_NoCatalog_CompletesWithoutError()
    {
        var fixture = ReleaseTicketsFixture.WithoutCatalog();
        await fixture.SetupAsync(Environment);

        var command = new ReleaseTicketsCommand(fixture.RegistrationId.Value, fixture.EventId.Value, fixture.TeamId.Value);
        var sut = new ReleaseTicketsHandler(Environment.RegistrationsDatabase.Context);

        // Should complete without throwing
        await sut.HandleAsync(command, testContext.CancellationToken);
    }

    // Given a ticket type with used capacity already at zero
    // When the tickets are released
    // Then the used capacity remains at zero instead of going negative
    // UsedCapacity does not go below zero
    [TestMethod]
    public async ValueTask ReleaseTickets_UsedCapacityAtZero_RemainsAtZero()
    {
        var fixture = ReleaseTicketsFixture.WithCatalogAtZeroCapacity();
        await fixture.SetupAsync(Environment);

        var command = new ReleaseTicketsCommand(fixture.RegistrationId.Value, fixture.EventId.Value, fixture.TeamId.Value);
        var sut = new ReleaseTicketsHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.GetTicketType(fixture.TicketTypeId)!.UsedCapacity.ShouldBe(0);
        });
    }

    // Given a registration referencing a ticket type ID that is unknown to the catalog
    // When the tickets are released
    // Then the unknown ticket type is silently skipped and known ticket types are unaffected
    // Unknown ticket type IDs are silently skipped
    [TestMethod]
    public async ValueTask ReleaseTickets_UnknownSlug_IsSkippedWithoutError()
    {
        var fixture = ReleaseTicketsFixture.WithCatalogAndUnknownTicketTypeInRegistration();
        await fixture.SetupAsync(Environment);

        var command = new ReleaseTicketsCommand(fixture.RegistrationId.Value, fixture.EventId.Value, fixture.TeamId.Value);
        var sut = new ReleaseTicketsHandler(Environment.RegistrationsDatabase.Context);

        // Should complete without throwing; unknown ticket type is silently skipped
        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            // The known ticket type's capacity was not affected
            catalog.GetTicketType(fixture.KnownTicketTypeId)!.UsedCapacity.ShouldBe(1);
        });
    }
}
