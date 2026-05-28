using Amolenk.Admitto.Core.Organization.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.MaterializeTicketedEvent;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.MaterializeTicketedEvent.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.MaterializeTicketedEvent;

[TestClass]
public sealed class MaterializeTicketedEventTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: Happy-path materialisation creates aggregate + catalog and raises TicketedEventCreatedDomainEvent.
    // The domain event is converted to TicketedEventCreatedIntegrationEvent by RegistrationsIntegrationEventPublisher
    // (see RegistrationsIntegrationEventPublisherTests for that coverage).
    [TestMethod]
    public async ValueTask Materialize_NewRequest_CreatesAggregateAndRaisesDomainEvent()
    {
        var fixture = MaterializeTicketedEventFixture.New();
        await fixture.SetupAsync(Environment);

        var evt = new TicketedEventCreationRequestedIntegrationEvent(
            fixture.CreationRequestId,
            fixture.TeamId.Value,
            "My Conference",
            "https://conf.example.com",
            "https://tickets.example.com",
            DateTimeOffset.UtcNow.AddDays(30),
            DateTimeOffset.UtcNow.AddDays(31),
            "UTC");

        var sut = new TicketedEventCreationRequestedIntegrationEventHandler(
            new MaterializeTicketedEventHandler(Environment.RegistrationsDatabase.Context));

        await sut.HandleAsync(evt, testContext.CancellationToken);

        // Verify the TicketedEventCreatedDomainEvent is raised before SaveChanges dispatches it.
        var addedEntry = Environment.RegistrationsDatabase.Context.ChangeTracker
            .Entries<TicketedEvent>()
            .Single();
        var domainEvents = addedEntry.Entity.GetDomainEvents();
        var domainEvent = domainEvents.ShouldHaveSingleItem().ShouldBeOfType<TicketedEventCreatedDomainEvent>();
        domainEvent.CreationRequestId.Value.ShouldBe(fixture.CreationRequestId);
        domainEvent.TeamId.ShouldBe(fixture.TeamId);

        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.TeamId == fixture.TeamId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.TeamId.ShouldBe(fixture.TeamId);

            var catalog = await ctx.TicketCatalogs
                .FirstOrDefaultAsync(tc => tc.Id == te.Id, testContext.CancellationToken);
            catalog.ShouldNotBeNull();
        });
    }
}
