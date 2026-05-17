using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ReleaseTickets;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.ReleaseTickets;

[TestClass]
public sealed class ReleaseTicketsTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Cancelling a registration decrements UsedCapacity on matching ticket types
    [TestMethod]
    public async ValueTask ReleaseTickets_WithMatchingCatalog_DecrementsUsedCapacity()
    {
        var fixture = ReleaseTicketsFixture.WithCatalogAndRegistration(maxCapacity: 10, usedCapacity: 3);
        await fixture.SetupAsync(Environment);

        var command = new ReleaseTicketsCommand(fixture.RegistrationId.Value, fixture.EventId.Value);
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

    // Release is skipped when no ticket catalog exists (coupon-only registration)
    [TestMethod]
    public async ValueTask ReleaseTickets_NoCatalog_CompletesWithoutError()
    {
        var fixture = ReleaseTicketsFixture.WithoutCatalog();
        await fixture.SetupAsync(Environment);

        var command = new ReleaseTicketsCommand(fixture.RegistrationId.Value, fixture.EventId.Value);
        var sut = new ReleaseTicketsHandler(Environment.RegistrationsDatabase.Context);

        // Should complete without throwing
        await sut.HandleAsync(command, testContext.CancellationToken);
    }

    // UsedCapacity does not go below zero
    [TestMethod]
    public async ValueTask ReleaseTickets_UsedCapacityAtZero_RemainsAtZero()
    {
        var fixture = ReleaseTicketsFixture.WithCatalogAtZeroCapacity();
        await fixture.SetupAsync(Environment);

        var command = new ReleaseTicketsCommand(fixture.RegistrationId.Value, fixture.EventId.Value);
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

    // Unknown ticket type IDs are silently skipped
    [TestMethod]
    public async ValueTask ReleaseTickets_UnknownSlug_IsSkippedWithoutError()
    {
        var fixture = ReleaseTicketsFixture.WithCatalogAndUnknownTicketTypeInRegistration();
        await fixture.SetupAsync(Environment);

        var command = new ReleaseTicketsCommand(fixture.RegistrationId.Value, fixture.EventId.Value);
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
