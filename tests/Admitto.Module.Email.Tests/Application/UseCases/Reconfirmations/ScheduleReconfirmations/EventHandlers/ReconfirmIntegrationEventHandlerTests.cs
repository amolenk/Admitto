using Amolenk.Admitto.Module.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations;
using Amolenk.Admitto.Module.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations.EventHandlers;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Module.Email.Tests.Application.UseCases.Reconfirmations.ScheduleReconfirmations.EventHandlers;

[TestClass]
public sealed class ReconfirmIntegrationEventHandlerTests
{
    private static readonly DateTimeOffset Opens = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Closes = new(2030, 12, 31, 0, 0, 0, TimeSpan.Zero);

    private static ReconfirmTriggerSpecDto Spec(Guid teamId, Guid eventId) =>
        new(teamId, eventId, "UTC", Opens, Closes, 1);

    [TestMethod]
    public async Task TicketedEventCreated_WithExistingPolicy_DispatchesUpsertCommand()
    {
        var teamId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var spec = Spec(teamId, eventId);

        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetReconfirmTriggerSpecAsync(TicketedEventId.From(eventId), Arg.Any<CancellationToken>())
            .Returns(spec);
        var mediator = Substitute.For<IMediator>();

        var handler = new TicketedEventCreatedIntegrationEventHandler(facade, mediator);

        await handler.HandleAsync(
            new TicketedEventCreated(Guid.NewGuid(), teamId, eventId, "slug", "UTC"),
            default);

        await mediator.Received(1).SendAsync(
            Arg.Is<ScheduleReconfirmationsCommand>(c =>
                c.TicketedEventId == TicketedEventId.From(eventId) && c.Spec == spec),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task TicketedEventCreated_WithoutPolicy_DoesNotDispatch()
    {
        var eventId = Guid.NewGuid();
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetReconfirmTriggerSpecAsync(TicketedEventId.From(eventId), Arg.Any<CancellationToken>())
            .Returns((ReconfirmTriggerSpecDto?)null);
        var mediator = Substitute.For<IMediator>();

        var handler = new TicketedEventCreatedIntegrationEventHandler(facade, mediator);

        await handler.HandleAsync(
            new TicketedEventCreated(Guid.NewGuid(), Guid.NewGuid(), eventId, "slug", "UTC"),
            default);

        await mediator.DidNotReceive().SendAsync(
            Arg.Any<ScheduleReconfirmationsCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ReconfirmPolicyChanged_WithPolicy_DispatchesUpsertCommand()
    {
        var teamId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var spec = Spec(teamId, eventId);

        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetReconfirmTriggerSpecAsync(TicketedEventId.From(eventId), Arg.Any<CancellationToken>())
            .Returns(spec);
        var mediator = Substitute.For<IMediator>();

        var handler = new TicketedEventReconfirmPolicyChangedIntegrationEventHandler(facade, mediator);

        await handler.HandleAsync(
            new TicketedEventReconfirmPolicyChangedIntegrationEvent(
                teamId, eventId,
                new TicketedEventReconfirmPolicySnapshot(Opens, Closes, 1)),
            default);

        await mediator.Received(1).SendAsync(
            Arg.Is<ScheduleReconfirmationsCommand>(c =>
                c.TicketedEventId == TicketedEventId.From(eventId) && c.Spec == spec),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ReconfirmPolicyChanged_PolicyCleared_DispatchesRemoveCommand()
    {
        var eventId = Guid.NewGuid();
        var facade = Substitute.For<IRegistrationsFacade>();
        var mediator = Substitute.For<IMediator>();

        var handler = new TicketedEventReconfirmPolicyChangedIntegrationEventHandler(facade, mediator);

        await handler.HandleAsync(
            new TicketedEventReconfirmPolicyChangedIntegrationEvent(Guid.NewGuid(), eventId, Policy: null),
            default);

        await mediator.Received(1).SendAsync(
            Arg.Is<ScheduleReconfirmationsCommand>(c =>
                c.TicketedEventId == TicketedEventId.From(eventId) && c.Spec == null),
            Arg.Any<CancellationToken>());
        await facade.DidNotReceive().GetReconfirmTriggerSpecAsync(
            Arg.Any<TicketedEventId>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task TimeZoneChanged_WithPolicy_DispatchesUpsertCommand()
    {
        var teamId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var spec = Spec(teamId, eventId);

        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetReconfirmTriggerSpecAsync(TicketedEventId.From(eventId), Arg.Any<CancellationToken>())
            .Returns(spec);
        var mediator = Substitute.For<IMediator>();

        var handler = new TicketedEventTimeZoneChangedIntegrationEventHandler(facade, mediator);

        await handler.HandleAsync(
            new TicketedEventTimeZoneChangedIntegrationEvent(teamId, eventId, "Europe/Amsterdam"),
            default);

        await mediator.Received(1).SendAsync(
            Arg.Is<ScheduleReconfirmationsCommand>(c =>
                c.TicketedEventId == TicketedEventId.From(eventId) && c.Spec == spec),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task TimeZoneChanged_WithoutPolicy_DoesNotDispatch()
    {
        var eventId = Guid.NewGuid();
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetReconfirmTriggerSpecAsync(TicketedEventId.From(eventId), Arg.Any<CancellationToken>())
            .Returns((ReconfirmTriggerSpecDto?)null);
        var mediator = Substitute.For<IMediator>();

        var handler = new TicketedEventTimeZoneChangedIntegrationEventHandler(facade, mediator);

        await handler.HandleAsync(
            new TicketedEventTimeZoneChangedIntegrationEvent(Guid.NewGuid(), eventId, "UTC"),
            default);

        await mediator.DidNotReceive().SendAsync(
            Arg.Any<ScheduleReconfirmationsCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task TicketedEventArchived_DispatchesRemoveCommand()
    {
        var eventId = Guid.NewGuid();
        var mediator = Substitute.For<IMediator>();

        var handler = new TicketedEventArchivedReconfirmIntegrationEventHandler(mediator);

        await handler.HandleAsync(
            new TicketedEventArchived(Guid.NewGuid(), eventId, "slug"),
            default);

        await mediator.Received(1).SendAsync(
            Arg.Is<ScheduleReconfirmationsCommand>(c =>
                c.TicketedEventId == TicketedEventId.From(eventId) && c.Spec == null),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task TicketedEventCancelled_DispatchesRemoveCommand()
    {
        var eventId = Guid.NewGuid();
        var mediator = Substitute.For<IMediator>();

        var handler = new TicketedEventCancelledReconfirmIntegrationEventHandler(mediator);

        await handler.HandleAsync(
            new TicketedEventCancelled(Guid.NewGuid(), eventId, "slug"),
            default);

        await mediator.Received(1).SendAsync(
            Arg.Is<ScheduleReconfirmationsCommand>(c =>
                c.TicketedEventId == TicketedEventId.From(eventId) && c.Spec == null),
            Arg.Any<CancellationToken>());
    }
}
