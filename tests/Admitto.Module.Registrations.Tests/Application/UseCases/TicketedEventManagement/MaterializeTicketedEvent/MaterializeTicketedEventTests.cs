using Amolenk.Admitto.Module.Organization.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.MaterializeTicketedEvent.EventHandlers;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.MaterializeTicketedEvent;

[TestClass]
public sealed class MaterializeTicketedEventTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: Happy-path materialisation creates aggregate + catalog and outboxes TicketedEventCreated
    [TestMethod]
    public async ValueTask SC001_Materialize_NewRequest_CreatesAggregateAndOutboxesCreated()
    {
        var fixture = MaterializeTicketedEventFixture.NoExistingEvent();
        await fixture.SetupAsync(Environment);

        var evt = new TicketedEventCreationRequested(
            fixture.CreationRequestId,
            fixture.TeamId.Value,
            fixture.Slug,
            "My Conference",
            "https://conf.example.com",
            "https://tickets.example.com",
            DateTimeOffset.UtcNow.AddDays(30),
            DateTimeOffset.UtcNow.AddDays(31),
            "UTC");

        var sut = new MaterializeTicketedEventIntegrationEventHandler(
            Environment.Database.Context,
            new IntegrationEventOutbox(Environment.Database.Context));

        await sut.HandleAsync(evt, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Slug == Amolenk.Admitto.Module.Shared.Kernel.ValueObjects.Slug.From(fixture.Slug),
                    testContext.CancellationToken);
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

    // SC-002: Duplicate slug outboxes TicketedEventCreationRejected with reason duplicate_slug
    [TestMethod]
    public async ValueTask SC002_Materialize_DuplicateSlug_OutboxesRejected()
    {
        var fixture = MaterializeTicketedEventFixture.WithConflictingSlug();
        await fixture.SetupAsync(Environment);

        var evt = new TicketedEventCreationRequested(
            fixture.CreationRequestId,
            fixture.TeamId.Value,
            fixture.Slug,
            "My Conference",
            "https://conf.example.com",
            "https://tickets.example.com",
            DateTimeOffset.UtcNow.AddDays(30),
            DateTimeOffset.UtcNow.AddDays(31),
            "UTC");

        var sut = new MaterializeTicketedEventIntegrationEventHandler(
            Environment.Database.Context,
            new IntegrationEventOutbox(Environment.Database.Context));

        await sut.HandleAsync(evt, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async ctx =>
        {
            var outbox = await ctx.OutboxMessages
                .Where(m => m.Type == "integration.registrations.ticketed-event-creation-rejected")
                .ToListAsync(testContext.CancellationToken);
            outbox.ShouldHaveSingleItem();

            var payload = outbox[0].Payload.RootElement;
            var json = payload.GetRawText();
            payload.TryGetProperty("reason", out var reasonEl).ShouldBeTrue($"payload: {json}");
            reasonEl.GetString().ShouldBe("duplicate_slug");
        });
    }

    // SC-003: Redelivery after successful materialisation yields duplicate_slug rejection (idempotent safe)
    [TestMethod]
    public async ValueTask SC003_Materialize_Redelivery_EmitsDuplicateSlugRejection()
    {
        var fixture = MaterializeTicketedEventFixture.NoExistingEvent();
        await fixture.SetupAsync(Environment);

        var evt = new TicketedEventCreationRequested(
            fixture.CreationRequestId,
            fixture.TeamId.Value,
            fixture.Slug,
            "My Conference",
            "https://conf.example.com",
            "https://tickets.example.com",
            DateTimeOffset.UtcNow.AddDays(30),
            DateTimeOffset.UtcNow.AddDays(31),
            "UTC");

        var sut = new MaterializeTicketedEventIntegrationEventHandler(
            Environment.Database.Context,
            new IntegrationEventOutbox(Environment.Database.Context));

        await sut.HandleAsync(evt, testContext.CancellationToken);
        await Environment.Database.Context.SaveChangesAsync(testContext.CancellationToken);

        await sut.HandleAsync(evt, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async ctx =>
        {
            var rejected = await ctx.OutboxMessages
                .Where(m => m.Type == "integration.registrations.ticketed-event-creation-rejected")
                .ToListAsync(testContext.CancellationToken);
            rejected.ShouldHaveSingleItem();

            var events = await ctx.TicketedEvents.CountAsync(testContext.CancellationToken);
            events.ShouldBe(1);
        });
    }
}
