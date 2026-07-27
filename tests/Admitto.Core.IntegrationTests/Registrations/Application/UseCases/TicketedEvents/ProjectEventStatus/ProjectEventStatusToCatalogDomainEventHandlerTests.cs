using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ProjectEventStatus.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.ProjectEventStatus;

[TestClass]
public sealed class ProjectEventStatusToCatalogDomainEventHandlerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask Archived_ProjectsOntoCatalog()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();
        await Environment.RegistrationsDatabase.SeedAsync(db =>
        {
            var catalog = TicketCatalog.Create(eventId, teamId);
            db.TicketCatalogs.Add(catalog);
        });

        var handler = new TicketedEventStatusChangedDomainEventHandler(Environment.RegistrationsDatabase.Context);
        var domainEvent = new TicketedEventStatusChangedDomainEvent(
            eventId, teamId, TicketedEventVersion: 1, EventLifecycleStatus.Archived);

        await handler.HandleAsync(domainEvent, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var catalog = await db.TicketCatalogs
                .FirstOrDefaultAsync(c => c.Id == eventId, testContext.CancellationToken);
            catalog.ShouldNotBeNull();
            catalog.EventStatus.ShouldBe(EventLifecycleStatus.Archived);
        });
    }

    [TestMethod]
    public async ValueTask NoCatalog_NoOp()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();

        var handler = new TicketedEventStatusChangedDomainEventHandler(Environment.RegistrationsDatabase.Context);
        var domainEvent = new TicketedEventStatusChangedDomainEvent(
            eventId, teamId, TicketedEventVersion: 1, EventLifecycleStatus.Archived);

        // Should complete without throwing even when no catalog exists yet.
        await handler.HandleAsync(domainEvent, testContext.CancellationToken);
    }
}
