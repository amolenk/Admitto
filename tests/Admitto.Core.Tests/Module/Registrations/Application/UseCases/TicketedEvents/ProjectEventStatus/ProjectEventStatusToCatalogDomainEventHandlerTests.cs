using Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketedEvents.ProjectEventStatus.EventHandlers;
using Amolenk.Admitto.Core.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Module.Registrations.Tests.Application.UseCases.TicketedEvents.ProjectEventStatus;

[TestClass]
public sealed class ProjectEventStatusToCatalogDomainEventHandlerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC001_Cancelled_ProjectsOntoCatalog()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();
        await Environment.Database.SeedAsync(db =>
        {
            var catalog = TicketCatalog.Create(eventId);
            db.TicketCatalogs.Add(catalog);
        });

        var handler = new TicketedEventStatusChangedDomainEventHandler(Environment.Database.Context);
        var domainEvent = new TicketedEventStatusChangedDomainEvent(
            eventId, teamId, EventLifecycleStatus.Cancelled);

        await handler.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async db =>
        {
            var catalog = await db.TicketCatalogs
                .FirstOrDefaultAsync(c => c.Id == eventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.EventStatus.ShouldBe(EventLifecycleStatus.Cancelled);
        });
    }

    [TestMethod]
    public async ValueTask SC002_Archived_ProjectsOntoCatalog()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();
        await Environment.Database.SeedAsync(db =>
        {
            var catalog = TicketCatalog.Create(eventId);
            db.TicketCatalogs.Add(catalog);
        });

        var handler = new TicketedEventStatusChangedDomainEventHandler(Environment.Database.Context);
        var domainEvent = new TicketedEventStatusChangedDomainEvent(
            eventId, teamId, EventLifecycleStatus.Archived);

        await handler.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async db =>
        {
            var catalog = await db.TicketCatalogs
                .FirstOrDefaultAsync(c => c.Id == eventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.EventStatus.ShouldBe(EventLifecycleStatus.Archived);
        });
    }

    [TestMethod]
    public async ValueTask SC003_NoCatalog_NoOp()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();

        var handler = new TicketedEventStatusChangedDomainEventHandler(Environment.Database.Context);
        var domainEvent = new TicketedEventStatusChangedDomainEvent(
            eventId, teamId, EventLifecycleStatus.Cancelled);

        // Should complete without throwing even when no catalog exists yet.
        await handler.HandleAsync(domainEvent, testContext.CancellationToken);
    }
}
