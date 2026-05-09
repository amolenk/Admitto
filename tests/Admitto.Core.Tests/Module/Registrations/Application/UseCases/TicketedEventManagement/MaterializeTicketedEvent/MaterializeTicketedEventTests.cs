using Amolenk.Admitto.Core.Organization.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.MaterializeTicketedEvent.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Registrations.Tests.Application.UseCases.TicketedEventManagement.MaterializeTicketedEvent;

[TestClass]
public sealed class MaterializeTicketedEventTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: Happy-path materialisation creates aggregate + catalog and outboxes TicketedEventCreatedIntegrationEvent
    [TestMethod]
    public async ValueTask SC001_Materialize_NewRequest_CreatesAggregateAndOutboxesCreated()
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
            Environment.Database.Context,
            new IntegrationEventOutbox(Environment.Database.Context));

        await sut.HandleAsync(evt, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.TeamId == fixture.TeamId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.TeamId.ShouldBe(fixture.TeamId);

            var catalog = await ctx.TicketCatalogs
                .FirstOrDefaultAsync(tc => tc.Id == te.Id, testContext.CancellationToken);
            catalog.ShouldNotBeNull();

            var outbox = await ctx.OutboxMessages
                .Where(m => m.Type == "integration.registrations.ticketed-event-created")
                .ToListAsync(testContext.CancellationToken);
            outbox.ShouldHaveSingleItem();
        });
    }
}
