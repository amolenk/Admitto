using Amolenk.Admitto.Module.Registrations.Application.Messaging;
using Amolenk.Admitto.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Module.Registrations.Domain.Tests.Messaging;

[TestClass]
public sealed class RegistrationsMessagePolicyTests
{
    private readonly RegistrationsMessagePolicy _sut = new();

    [TestMethod]
    public void SC001_Policy_AttendeeRegisteredDomainEvent_PublishesIntegrationEvent()
    {
        // Arrange
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var registrationId = RegistrationId.New();
        var email = EmailAddress.From("test@example.com");
        var domainEvent = new AttendeeRegisteredDomainEvent(teamId, eventId, registrationId, email, "Attendee");

        // Act
        var shouldPublish = _sut.ShouldPublishIntegrationEvent(domainEvent);
        var integrationEvent = (AttendeeRegisteredIntegrationEvent)_sut.MapToIntegrationEvent(domainEvent);

        // Assert
        shouldPublish.ShouldBeTrue();
        integrationEvent.TeamId.ShouldBe(teamId.Value);
        integrationEvent.TicketedEventId.ShouldBe(eventId.Value);
        integrationEvent.RegistrationId.ShouldBe(registrationId.Value);
        integrationEvent.RecipientEmail.ShouldBe(email.Value);
        integrationEvent.RecipientName.ShouldBe("Attendee");
    }
}
