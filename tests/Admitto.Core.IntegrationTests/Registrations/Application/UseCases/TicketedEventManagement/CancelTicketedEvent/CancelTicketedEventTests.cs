using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.CancelTicketedEvent;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEventManagement.CancelTicketedEvent;

[TestClass]
public sealed class CancelTicketedEventTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: Cancel active event — transitions Status to Cancelled
    [TestMethod]
    public async ValueTask CancelTicketedEvent_ActiveEvent_TransitionsToCancelled()
    {
        var fixture = CancelTicketedEventFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var sut = new CancelTicketedEventHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(new CancelTicketedEventCommand(fixture.EventId.Value), testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.Status.ShouldBe(EventLifecycleStatus.Cancelled);
        });
    }

    // SC-002: Cancel already-cancelled event throws
    [TestMethod]
    public async ValueTask CancelTicketedEvent_AlreadyCancelled_ThrowsAlreadyCancelled()
    {
        var fixture = CancelTicketedEventFixture.AlreadyCancelled();
        await fixture.SetupAsync(Environment);

        var sut = new CancelTicketedEventHandler(Environment.RegistrationsDatabase.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(new CancelTicketedEventCommand(fixture.EventId.Value), testContext.CancellationToken));

        result.Error.Code.ShouldBe("ticketed_event.already_cancelled");
    }

    // SC-003: Cancel archived event throws
    [TestMethod]
    public async ValueTask CancelTicketedEvent_AlreadyArchived_ThrowsAlreadyArchived()
    {
        var fixture = CancelTicketedEventFixture.AlreadyArchived();
        await fixture.SetupAsync(Environment);

        var sut = new CancelTicketedEventHandler(Environment.RegistrationsDatabase.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(new CancelTicketedEventCommand(fixture.EventId.Value), testContext.CancellationToken));

        result.Error.Code.ShouldBe("ticketed_event.already_archived");
    }
}
