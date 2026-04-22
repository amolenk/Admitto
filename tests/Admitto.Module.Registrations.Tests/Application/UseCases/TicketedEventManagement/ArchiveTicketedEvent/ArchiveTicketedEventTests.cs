using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ArchiveTicketedEvent;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.ArchiveTicketedEvent;

[TestClass]
public sealed class ArchiveTicketedEventTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: Archive active event — transitions Status to Archived
    [TestMethod]
    public async ValueTask SC001_ArchiveTicketedEvent_ActiveEvent_TransitionsToArchived()
    {
        var fixture = ArchiveTicketedEventFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var sut = new ArchiveTicketedEventHandler(Environment.Database.Context);

        await sut.HandleAsync(new ArchiveTicketedEventCommand(fixture.EventId), testContext.CancellationToken);

        await Environment.Database.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.Status.ShouldBe(EventLifecycleStatus.Archived);
        });
    }

    // SC-002: Archive cancelled event — allowed, transitions to Archived
    [TestMethod]
    public async ValueTask SC002_ArchiveTicketedEvent_CancelledEvent_TransitionsToArchived()
    {
        var fixture = ArchiveTicketedEventFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var sut = new ArchiveTicketedEventHandler(Environment.Database.Context);

        await sut.HandleAsync(new ArchiveTicketedEventCommand(fixture.EventId), testContext.CancellationToken);

        await Environment.Database.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.Status.ShouldBe(EventLifecycleStatus.Archived);
        });
    }

    // SC-003: Archive already-archived event throws
    [TestMethod]
    public async ValueTask SC003_ArchiveTicketedEvent_AlreadyArchived_ThrowsAlreadyArchived()
    {
        var fixture = ArchiveTicketedEventFixture.AlreadyArchived();
        await fixture.SetupAsync(Environment);

        var sut = new ArchiveTicketedEventHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(new ArchiveTicketedEventCommand(fixture.EventId), testContext.CancellationToken));

        result.Error.Code.ShouldBe("ticketed_event.already_archived");
    }
}
