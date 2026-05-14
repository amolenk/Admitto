using Amolenk.Admitto.Core.Registrations.Application.Messaging;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using NSubstitute;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Tests.Application.Messaging;

[TestClass]
public sealed class RegistrationsIntegrationEventPublisherTests
{
    private IIntegrationEvent? _captured;
    private IOutbox _outbox = null!;
    private RegistrationsIntegrationEventPublisher _publisher = null!;

    [TestInitialize]
    public void Initialize()
    {
        _outbox = Substitute.For<IOutbox>();
        _outbox.When(o => o.Enqueue(Arg.Any<IIntegrationEvent>())).Do(ci => _captured = ci.Arg<IIntegrationEvent>());
        _publisher = new RegistrationsIntegrationEventPublisher(_outbox);
    }

    [TestMethod]
    public async ValueTask AttendeeRegistered_EnqueuesAttendeeRegisteredIntegrationEvent()
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
            [new TicketTypeSnapshot(Slug.From("early-bird"), TicketTypeName.From("Early Bird"), [])]);

        await _publisher.HandleAsync(domainEvent, CancellationToken.None);

        _captured.ShouldNotBeNull();
        var evt = _captured.ShouldBeOfType<AttendeeRegisteredIntegrationEvent>();
        evt.RegistrationId.ShouldBe(registrationId.Value);
        evt.TeamId.ShouldBe(teamId.Value);
        evt.RecipientEmail.ShouldBe("bob@example.com");
        evt.FirstName.ShouldBe("Bob");
        evt.LastName.ShouldBe("Smith");
        evt.Tickets.ShouldHaveSingleItem().Slug.ShouldBe("early-bird");
    }

    [TestMethod]
    public async ValueTask OtpCodeRequested_EnqueuesOtpCodeRequestedIntegrationEvent()
    {
        var otpCodeId = OtpCodeId.New();
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var domainEvent = new OtpCodeRequestedDomainEvent(
            otpCodeId,
            teamId,
            eventId,
            EventName: EventName.From("Spring Conf"),
            RecipientEmail: EmailAddress.From("alice@example.com"),
            PlainCode: "123456");

        await _publisher.HandleAsync(domainEvent, CancellationToken.None);

        _captured.ShouldNotBeNull();
        var evt = _captured.ShouldBeOfType<OtpCodeRequestedIntegrationEvent>();
        evt.OtpCodeId.ShouldBe(otpCodeId.Value);
        evt.TeamId.ShouldBe(teamId.Value);
        evt.TicketedEventId.ShouldBe(eventId.Value);
        evt.EventName.ShouldBe("Spring Conf");
        evt.RecipientEmail.ShouldBe("alice@example.com");
        evt.PlainCode.ShouldBe("123456");
    }

    [TestMethod]
    public async ValueTask RegistrationCancelled_EnqueuesRegistrationCancelledIntegrationEvent()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var registrationId = RegistrationId.New();

        var domainEvent = new RegistrationCancelledDomainEvent(
            teamId,
            eventId,
            registrationId,
            EmailAddress.From("carol@example.com"),
            CancellationReason.AttendeeRequest);

        await _publisher.HandleAsync(domainEvent, CancellationToken.None);

        _captured.ShouldNotBeNull();
        var evt = _captured.ShouldBeOfType<RegistrationCancelledIntegrationEvent>();
        evt.TeamId.ShouldBe(teamId.Value);
        evt.TicketedEventId.ShouldBe(eventId.Value);
        evt.RegistrationId.ShouldBe(registrationId.Value);
        evt.RecipientEmail.ShouldBe("carol@example.com");
        evt.Reason.ShouldBe(nameof(CancellationReason.AttendeeRequest));
    }

    [TestMethod]
    public async ValueTask RegistrationReconfirmed_EnqueuesRegistrationReconfirmedIntegrationEvent()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var registrationId = RegistrationId.New();
        var reconfirmedAt = DateTimeOffset.UtcNow;

        var domainEvent = new RegistrationReconfirmedDomainEvent(
            teamId,
            eventId,
            registrationId,
            EmailAddress.From("dave@example.com"),
            reconfirmedAt);

        await _publisher.HandleAsync(domainEvent, CancellationToken.None);

        _captured.ShouldNotBeNull();
        var evt = _captured.ShouldBeOfType<RegistrationReconfirmedIntegrationEvent>();
        evt.TeamId.ShouldBe(teamId.Value);
        evt.TicketedEventId.ShouldBe(eventId.Value);
        evt.RegistrationId.ShouldBe(registrationId.Value);
        evt.RecipientEmail.ShouldBe("dave@example.com");
        evt.ReconfirmedAt.ShouldBe(reconfirmedAt);
    }

    [TestMethod]
    public async ValueTask TicketedEventCreated_EnqueuesTicketedEventCreatedIntegrationEvent()
    {
        var creationRequestId = CreationRequestId.From(Guid.NewGuid());
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var domainEvent = new TicketedEventCreatedDomainEvent(
            creationRequestId,
            teamId,
            eventId,
            TimeZoneId.From("Europe/Amsterdam"));

        await _publisher.HandleAsync(domainEvent, CancellationToken.None);

        _captured.ShouldNotBeNull();
        var evt = _captured.ShouldBeOfType<TicketedEventCreatedIntegrationEvent>();
        evt.CreationRequestId.ShouldBe(creationRequestId.Value);
        evt.TeamId.ShouldBe(teamId.Value);
        evt.TicketedEventId.ShouldBe(eventId.Value);
        evt.TimeZone.ShouldBe("Europe/Amsterdam");
    }

    [TestMethod]
    public async ValueTask TicketedEventReconfirmPolicyChanged_EnqueuesIntegrationEvent()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var opensAt = DateTimeOffset.UtcNow.AddDays(1);
        var closesAt = DateTimeOffset.UtcNow.AddDays(10);

        var domainEvent = new TicketedEventReconfirmPolicyChangedDomainEvent(
            teamId,
            eventId,
            TicketedEventReconfirmPolicy.Create(opensAt, closesAt, TimeSpan.FromDays(7)));

        await _publisher.HandleAsync(domainEvent, CancellationToken.None);

        _captured.ShouldNotBeNull();
        var evt = _captured.ShouldBeOfType<TicketedEventReconfirmPolicyChangedIntegrationEvent>();
        evt.TeamId.ShouldBe(teamId.Value);
        evt.TicketedEventId.ShouldBe(eventId.Value);
        evt.Policy.ShouldNotBeNull();
        evt.Policy.OpensAt.ShouldBe(opensAt);
        evt.Policy.ClosesAt.ShouldBe(closesAt);
        evt.Policy.CadenceDays.ShouldBe(7);
    }

    [TestMethod]
    public async ValueTask TicketedEventReconfirmPolicyCleared_EnqueuesIntegrationEventWithNullPolicy()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var domainEvent = new TicketedEventReconfirmPolicyChangedDomainEvent(teamId, eventId, Policy: null);

        await _publisher.HandleAsync(domainEvent, CancellationToken.None);

        _captured.ShouldNotBeNull();
        var evt = _captured.ShouldBeOfType<TicketedEventReconfirmPolicyChangedIntegrationEvent>();
        evt.Policy.ShouldBeNull();
    }

    [TestMethod]
    public async ValueTask StatusChangedToCancelled_EnqueuesCancelledIntegrationEvent()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();
        var domainEvent = new TicketedEventStatusChangedDomainEvent(eventId, teamId, EventLifecycleStatus.Cancelled);

        await _publisher.HandleAsync(domainEvent, CancellationToken.None);

        var evt = _captured.ShouldBeOfType<TicketedEventCancelledIntegrationEvent>();
        evt.TicketedEventId.ShouldBe(eventId.Value);
        evt.TeamId.ShouldBe(teamId.Value);
    }

    [TestMethod]
    public async ValueTask StatusChangedToArchived_EnqueuesArchivedIntegrationEvent()
    {
        var eventId = TicketedEventId.New();
        var teamId = TeamId.New();
        var domainEvent = new TicketedEventStatusChangedDomainEvent(eventId, teamId, EventLifecycleStatus.Archived);

        await _publisher.HandleAsync(domainEvent, CancellationToken.None);

        var evt = _captured.ShouldBeOfType<TicketedEventArchivedIntegrationEvent>();
        evt.TicketedEventId.ShouldBe(eventId.Value);
        evt.TeamId.ShouldBe(teamId.Value);
    }

    [TestMethod]
    public async ValueTask TicketedEventTimeZoneChanged_EnqueuesTimeZoneChangedIntegrationEvent()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        var domainEvent = new TicketedEventTimeZoneChangedDomainEvent(
            teamId,
            eventId,
            TimeZoneId.From("Europe/Amsterdam"));

        await _publisher.HandleAsync(domainEvent, CancellationToken.None);

        _captured.ShouldNotBeNull();
        var evt = _captured.ShouldBeOfType<TicketedEventTimeZoneChangedIntegrationEvent>();
        evt.TeamId.ShouldBe(teamId.Value);
        evt.TicketedEventId.ShouldBe(eventId.Value);
        evt.TimeZone.ShouldBe("Europe/Amsterdam");
    }

    [TestMethod]
    public async ValueTask TicketsChanged_EnqueuesAttendeeTicketsChangedIntegrationEvent()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var registrationId = RegistrationId.New();
        var changedAt = DateTimeOffset.UtcNow;

        var domainEvent = new TicketsChangedDomainEvent(
            teamId,
            eventId,
            registrationId,
            EmailAddress.From("eve@example.com"),
            FirstName.From("Eve"),
            LastName.From("Adams"),
            [],
            [new TicketTypeSnapshot(Slug.From("vip"), TicketTypeName.From("VIP"), [])],
            changedAt);

        await _publisher.HandleAsync(domainEvent, CancellationToken.None);

        _captured.ShouldNotBeNull();
        var evt = _captured.ShouldBeOfType<AttendeeTicketsChangedIntegrationEvent>();
        evt.TeamId.ShouldBe(teamId.Value);
        evt.TicketedEventId.ShouldBe(eventId.Value);
        evt.RegistrationId.ShouldBe(registrationId.Value);
        evt.RecipientEmail.ShouldBe("eve@example.com");
        evt.FirstName.ShouldBe("Eve");
        evt.LastName.ShouldBe("Adams");
        evt.NewTickets.ShouldHaveSingleItem().Slug.ShouldBe("vip");
        evt.ChangedAt.ShouldBe(changedAt);
    }
}
