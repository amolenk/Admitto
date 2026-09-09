using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;

[TestClass]
public sealed class AttendeeTicketsChangedIntegrationEventHandlerTests
{
    // Given an attendee has changed their ticket selection
    // When the ticket-change event is processed
    // Then the attendee receives a ticket confirmation with the updated registration details
    [TestMethod]
    public async Task HandleAsync_TicketSelectionChanged_DelegatesRecipientAndIntent()
    {
        var fixture = AttendeeTicketsChangedIntegrationEventHandlerFixture.Create();

        await new AttendeeTicketsChangedIntegrationEventHandler(fixture.Composer, fixture.DeliveryHandler)
            .HandleAsync(fixture.IntegrationEvent, CancellationToken.None);

        var arguments = fixture.Composer.ReceivedCalls().Single().GetArguments();
        var intent = (TicketConfirmationIntent)arguments[0]!;
        intent.FirstName.ShouldBe("Alice");
        intent.TicketTypes.ShouldBe(["General Admission"]);
        intent.RegistrationId.Value.ShouldBe(AttendeeTicketsChangedIntegrationEventHandlerFixture.RegistrationGuid);

        var delivery = fixture.DeliveryHandler.ReceivedDelivery();
        delivery.RecipientAddress.ShouldBe("alice@example.com");
        delivery.IdempotencyKey.ShouldBe(
            $"tickets-changed:{fixture.IntegrationEvent.RegistrationId}:{fixture.ChangedAt.ToUnixTimeMilliseconds()}");
        delivery.EmailType.ShouldBe(BuiltInEmailTemplateNames.TicketConfirmation);
        delivery.RegistrationId.ShouldBe(AttendeeTicketsChangedIntegrationEventHandlerFixture.RegistrationGuid);
    }
}
