using Amolenk.Admitto.Core.Registrations.Application.Messaging.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.Registrations.Tests.Application.Messaging.EventHandlers;

[TestClass]
public sealed class OtpCodeRequestedDomainEventHandlerTests
{
    [TestMethod]
    public async ValueTask SC001_OtpCodeRequested_EnqueuesOtpCodeRequestedIntegrationEvent()
    {
        var otpCodeId = OtpCodeId.New();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var domainEvent = new OtpCodeRequestedDomainEvent(
            otpCodeId,
            teamId,
            eventId,
            EventName: "Spring Conf",
            RecipientEmail: EmailAddress.From("alice@example.com"),
            PlainCode: "123456");

        IIntegrationEvent? captured = null;
        var outbox = Substitute.For<IOutbox>();
        outbox.When(o => o.Enqueue(Arg.Any<IIntegrationEvent>())).Do(ci => captured = ci.Arg<IIntegrationEvent>());

        var handler = new OtpCodeRequestedDomainEventHandler(outbox);
        await handler.HandleAsync(domainEvent, CancellationToken.None);

        captured.ShouldNotBeNull();
        var evt = captured.ShouldBeOfType<OtpCodeRequestedIntegrationEvent>();
        evt.OtpCodeId.ShouldBe(otpCodeId.Value);
        evt.TeamId.ShouldBe(teamId.Value);
        evt.TicketedEventId.ShouldBe(eventId.Value);
        evt.EventName.ShouldBe("Spring Conf");
        evt.RecipientEmail.ShouldBe("alice@example.com");
        evt.PlainCode.ShouldBe("123456");
    }
}

[TestClass]
public sealed class AttendeeRegisteredDomainEventHandlerTests
{
    [TestMethod]
    public async ValueTask SC001_AttendeeRegistered_EnqueuesAttendeeRegisteredIntegrationEvent()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var registrationId = RegistrationId.New();

        var domainEvent = new AttendeeRegisteredDomainEvent(
            teamId,
            eventId,
            registrationId,
            EmailAddress.From("bob@example.com"),
            FirstName.From("Bob"),
            LastName.From("Smith"),
            [new TicketTypeSnapshot("early-bird", "Early Bird", [])]);

        IIntegrationEvent? captured = null;
        var outbox = Substitute.For<IOutbox>();
        outbox.When(o => o.Enqueue(Arg.Any<IIntegrationEvent>())).Do(ci => captured = ci.Arg<IIntegrationEvent>());

        var handler = new AttendeeRegisteredDomainEventHandler(outbox);
        await handler.HandleAsync(domainEvent, CancellationToken.None);

        captured.ShouldNotBeNull();
        var evt = captured.ShouldBeOfType<AttendeeRegisteredIntegrationEvent>();
        evt.RegistrationId.ShouldBe(registrationId.Value);
        evt.TeamId.ShouldBe(teamId.Value);
        evt.RecipientEmail.ShouldBe("bob@example.com");
        evt.FirstName.ShouldBe("Bob");
        evt.LastName.ShouldBe("Smith");
        evt.Tickets.ShouldHaveSingleItem().Slug.ShouldBe("early-bird");
    }
}

[TestClass]
public sealed class TicketedEventStatusChangedDomainEventHandlerTests
{
    [TestMethod]
    public async ValueTask SC001_StatusChangedToCancelled_EnqueuesCancelledIntegrationEvent()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();
        var domainEvent = new TicketedEventStatusChangedDomainEvent(eventId, teamId, EventLifecycleStatus.Cancelled);

        IIntegrationEvent? captured = null;
        var outbox = Substitute.For<IOutbox>();
        outbox.When(o => o.Enqueue(Arg.Any<IIntegrationEvent>())).Do(ci => captured = ci.Arg<IIntegrationEvent>());

        var handler = new TicketedEventStatusChangedDomainEventHandler(outbox);
        await handler.HandleAsync(domainEvent, CancellationToken.None);

        var evt = captured.ShouldBeOfType<TicketedEventCancelledIntegrationEvent>();
        evt.TicketedEventId.ShouldBe(eventId.Value);
        evt.TeamId.ShouldBe(teamId.Value);
    }

    [TestMethod]
    public async ValueTask SC002_StatusChangedToArchived_EnqueuesArchivedIntegrationEvent()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();
        var domainEvent = new TicketedEventStatusChangedDomainEvent(eventId, teamId, EventLifecycleStatus.Archived);

        IIntegrationEvent? captured = null;
        var outbox = Substitute.For<IOutbox>();
        outbox.When(o => o.Enqueue(Arg.Any<IIntegrationEvent>())).Do(ci => captured = ci.Arg<IIntegrationEvent>());

        var handler = new TicketedEventStatusChangedDomainEventHandler(outbox);
        await handler.HandleAsync(domainEvent, CancellationToken.None);

        var evt = captured.ShouldBeOfType<TicketedEventArchivedIntegrationEvent>();
        evt.TicketedEventId.ShouldBe(eventId.Value);
        evt.TeamId.ShouldBe(teamId.Value);
    }
}
