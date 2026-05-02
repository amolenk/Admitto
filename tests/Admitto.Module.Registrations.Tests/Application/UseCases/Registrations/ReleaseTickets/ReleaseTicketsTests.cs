using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.ReleaseTickets;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.ReleaseTickets;

[TestClass]
public sealed class ReleaseTicketsTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC001: Cancelling a registration decrements UsedCapacity on matching ticket types
    [TestMethod]
    public async ValueTask SC001_ReleaseTickets_WithMatchingCatalog_DecrementsUsedCapacity()
    {
        var fixture = ReleaseTicketsFixture.WithCatalogAndRegistration(maxCapacity: 10, usedCapacity: 3);
        await fixture.SetupAsync(Environment);

        var command = new ReleaseTicketsCommand(fixture.RegistrationId, fixture.EventId);
        var sut = new ReleaseTicketsHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.GetTicketType(fixture.TicketTypeSlug)!.UsedCapacity.ShouldBe(2);
        });
    }

    // SC002: Release is skipped when no ticket catalog exists (coupon-only registration)
    [TestMethod]
    public async ValueTask SC002_ReleaseTickets_NoCatalog_CompletesWithoutError()
    {
        var fixture = ReleaseTicketsFixture.WithoutCatalog();
        await fixture.SetupAsync(Environment);

        var command = new ReleaseTicketsCommand(fixture.RegistrationId, fixture.EventId);
        var sut = new ReleaseTicketsHandler(Environment.Database.Context);

        // Should complete without throwing
        await sut.HandleAsync(command, testContext.CancellationToken);
    }

    // SC003: UsedCapacity does not go below zero
    [TestMethod]
    public async ValueTask SC003_ReleaseTickets_UsedCapacityAtZero_RemainsAtZero()
    {
        var fixture = ReleaseTicketsFixture.WithCatalogAtZeroCapacity();
        await fixture.SetupAsync(Environment);

        var command = new ReleaseTicketsCommand(fixture.RegistrationId, fixture.EventId);
        var sut = new ReleaseTicketsHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.GetTicketType(fixture.TicketTypeSlug)!.UsedCapacity.ShouldBe(0);
        });
    }

    // SC004: Unknown ticket type slugs are silently skipped
    [TestMethod]
    public async ValueTask SC004_ReleaseTickets_UnknownSlug_IsSkippedWithoutError()
    {
        var fixture = ReleaseTicketsFixture.WithCatalogAndUnknownSlugInRegistration();
        await fixture.SetupAsync(Environment);

        var command = new ReleaseTicketsCommand(fixture.RegistrationId, fixture.EventId);
        var sut = new ReleaseTicketsHandler(Environment.Database.Context);

        // Should complete without throwing; unknown slug is silently skipped
        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var catalog = await dbContext.TicketCatalogs
                .FirstOrDefaultAsync(c => c.Id == fixture.EventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            // The known ticket type's capacity was not affected
            catalog.GetTicketType("known-ticket")!.UsedCapacity.ShouldBe(1);
        });
    }
}
