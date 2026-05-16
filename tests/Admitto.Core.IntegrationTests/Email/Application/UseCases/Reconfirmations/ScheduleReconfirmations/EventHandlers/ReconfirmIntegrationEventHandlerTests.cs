using Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations;
using Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations.EventHandlers;

[TestClass]
public sealed class ReconfirmIntegrationEventHandlerTests
{
    private static readonly DateTimeOffset Opens = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Closes = new(2030, 12, 31, 0, 0, 0, TimeSpan.Zero);

    private static ReconfirmTriggerSpecDto Spec(Guid teamId, Guid eventId) =>
        new(teamId, eventId, "UTC", Opens, Closes, 1, MinEmailIntervalHours: 24);

    [TestMethod]
    public async Task TicketedEventCreatedIntegrationEvent_WithExistingPolicy_DispatchesUpsertCommand()
    {
        var teamId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var spec = Spec(teamId, eventId);

        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetReconfirmTriggerSpecAsync(TicketedEventId.From(eventId), Arg.Any<CancellationToken>())
            .Returns(spec);
        var scheduleHandler = Substitute.For<ICommandHandler<ScheduleReconfirmationsCommand>>();

        var handler = new TicketedEventCreatedIntegrationEventHandler(facade, scheduleHandler);

        await handler.HandleAsync(
            new TicketedEventCreatedIntegrationEvent(Guid.NewGuid(), teamId, eventId, "UTC"),
            default);

        await scheduleHandler.Received(1).HandleAsync(
            Arg.Is<ScheduleReconfirmationsCommand>(c =>
                c.TicketedEventId == TicketedEventId.From(eventId) && c.Spec == spec),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task TicketedEventCreatedIntegrationEvent_WithoutPolicy_DoesNotDispatch()
    {
        var eventId = Guid.NewGuid();
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetReconfirmTriggerSpecAsync(TicketedEventId.From(eventId), Arg.Any<CancellationToken>())
            .Returns((ReconfirmTriggerSpecDto?)null);
        var scheduleHandler = Substitute.For<ICommandHandler<ScheduleReconfirmationsCommand>>();

        var handler = new TicketedEventCreatedIntegrationEventHandler(facade, scheduleHandler);

        await handler.HandleAsync(
            new TicketedEventCreatedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), eventId, "UTC"),
            default);

        await scheduleHandler.DidNotReceive().HandleAsync(
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
        var scheduleHandler = Substitute.For<ICommandHandler<ScheduleReconfirmationsCommand>>();

        var handler = new TicketedEventReconfirmPolicyChangedIntegrationEventHandler(facade, scheduleHandler);

        await handler.HandleAsync(
            new TicketedEventReconfirmPolicyChangedIntegrationEvent(
                teamId, eventId,
                new TicketedEventReconfirmPolicySnapshot(Opens, Closes, 1, MinEmailIntervalHours: 0)),
            default);

        await scheduleHandler.Received(1).HandleAsync(
            Arg.Is<ScheduleReconfirmationsCommand>(c =>
                c.TicketedEventId == TicketedEventId.From(eventId) && c.Spec == spec),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ReconfirmPolicyChanged_PolicyCleared_DispatchesRemoveCommand()
    {
        var eventId = Guid.NewGuid();
        var facade = Substitute.For<IRegistrationsFacade>();
        var scheduleHandler = Substitute.For<ICommandHandler<ScheduleReconfirmationsCommand>>();

        var handler = new TicketedEventReconfirmPolicyChangedIntegrationEventHandler(facade, scheduleHandler);

        await handler.HandleAsync(
            new TicketedEventReconfirmPolicyChangedIntegrationEvent(Guid.NewGuid(), eventId, Policy: null),
            default);

        await scheduleHandler.Received(1).HandleAsync(
            Arg.Is<ScheduleReconfirmationsCommand>(c =>
                c.TicketedEventId == TicketedEventId.From(eventId) && c.Spec == null),
            Arg.Any<CancellationToken>());
        await facade.DidNotReceiveWithAnyArgs().GetReconfirmTriggerSpecAsync(TicketedEventId.New(), default);
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
        var scheduleHandler = Substitute.For<ICommandHandler<ScheduleReconfirmationsCommand>>();

        var handler = new TicketedEventTimeZoneChangedIntegrationEventHandler(facade, scheduleHandler);

        await handler.HandleAsync(
            new TicketedEventTimeZoneChangedIntegrationEvent(teamId, eventId, "Europe/Amsterdam"),
            default);

        await scheduleHandler.Received(1).HandleAsync(
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
        var scheduleHandler = Substitute.For<ICommandHandler<ScheduleReconfirmationsCommand>>();

        var handler = new TicketedEventTimeZoneChangedIntegrationEventHandler(facade, scheduleHandler);

        await handler.HandleAsync(
            new TicketedEventTimeZoneChangedIntegrationEvent(Guid.NewGuid(), eventId, "UTC"),
            default);

        await scheduleHandler.DidNotReceive().HandleAsync(
            Arg.Any<ScheduleReconfirmationsCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task TicketedEventArchivedIntegrationEvent_DispatchesRemoveCommand()
    {
        var eventId = Guid.NewGuid();
        var scheduleHandler = Substitute.For<ICommandHandler<ScheduleReconfirmationsCommand>>();

        var handler = new TicketedEventArchivedIntegrationEventHandler(scheduleHandler);

        await handler.HandleAsync(
            new TicketedEventArchivedIntegrationEvent(Guid.NewGuid(), eventId),
            default);

        await scheduleHandler.Received(1).HandleAsync(
            Arg.Is<ScheduleReconfirmationsCommand>(c =>
                c.TicketedEventId == TicketedEventId.From(eventId) && c.Spec == null),
            Arg.Any<CancellationToken>());
    }

}
