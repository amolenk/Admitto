using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.ArchiveTicketedEvent;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEventManagement.ArchiveTicketedEvent;

[TestClass]
public sealed class ArchiveTicketedEventTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: Archive active event — transitions Status to Archived
    [TestMethod]
    public async ValueTask ArchiveTicketedEvent_ActiveEvent_TransitionsToArchived()
    {
        var fixture = ArchiveTicketedEventFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var sut = new ArchiveTicketedEventHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(new ArchiveTicketedEventCommand(fixture.EventId.Value), testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.Status.ShouldBe(EventLifecycleStatus.Archived);
        });
    }

    // SC-002: Archive cancelled event — allowed, transitions to Archived
    [TestMethod]
    public async ValueTask ArchiveTicketedEvent_CancelledEvent_TransitionsToArchived()
    {
        var fixture = ArchiveTicketedEventFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var sut = new ArchiveTicketedEventHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(new ArchiveTicketedEventCommand(fixture.EventId.Value), testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.Status.ShouldBe(EventLifecycleStatus.Archived);
        });
    }

    // SC-003: Archive already-archived event throws
    [TestMethod]
    public async ValueTask ArchiveTicketedEvent_AlreadyArchived_ThrowsAlreadyArchived()
    {
        var fixture = ArchiveTicketedEventFixture.AlreadyArchived();
        await fixture.SetupAsync(Environment);

        var sut = new ArchiveTicketedEventHandler(Environment.RegistrationsDatabase.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(new ArchiveTicketedEventCommand(fixture.EventId.Value), testContext.CancellationToken));

        result.Error.Code.ShouldBe("ticketed_event.already_archived");
    }
}
