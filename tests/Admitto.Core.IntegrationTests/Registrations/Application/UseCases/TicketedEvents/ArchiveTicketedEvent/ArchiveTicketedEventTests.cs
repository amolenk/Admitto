using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ArchiveTicketedEvent;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.ArchiveTicketedEvent;

[TestClass]
public sealed class ArchiveTicketedEventTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an active ticketed event
    // When the event is archived
    // Then its status transitions to Archived
    [TestMethod]
    public async ValueTask ArchiveTicketedEvent_ActiveEvent_TransitionsToArchived()
    {
        var fixture = ArchiveTicketedEventFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var sut = new ArchiveTicketedEventHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(new ArchiveTicketedEventCommand(fixture.EventId.Value, fixture.TeamId.Value), testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.Status.ShouldBe(EventLifecycleStatus.Archived);
        });
    }

    // Given an already-archived ticketed event
    // When the event is archived again
    // Then it fails with an event-not-active error
    [TestMethod]
    public async ValueTask ArchiveTicketedEvent_AlreadyArchived_ThrowsEventNotActive()
    {
        var fixture = ArchiveTicketedEventFixture.AlreadyArchived();
        await fixture.SetupAsync(Environment);

        var sut = new ArchiveTicketedEventHandler(Environment.RegistrationsDatabase.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(new ArchiveTicketedEventCommand(fixture.EventId.Value, fixture.TeamId.Value), testContext.CancellationToken));

        result.Error.ShouldMatch(TicketedEvent.Errors.EventNotActive);
    }
}
