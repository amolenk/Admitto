using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTicketConfirmation;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

[TestClass]
public sealed class AttendeeTicketsChangedIntegrationEventHandlerTests
{
    // Given an attendee ticket-change event
    // When the event is handled
    // Then the common composer receives the ticket intent and exact delivery identity
    [TestMethod]
    public async Task HandleAsync_TicketSelectionChanged_DelegatesRecipientAndIntent()
    {
        var fixture = AttendeeTicketsChangedIntegrationEventHandlerFixture.Create();

        await new AttendeeTicketsChangedIntegrationEventHandler(fixture.Composer)
            .HandleAsync(fixture.IntegrationEvent, CancellationToken.None);

        var arguments = fixture.Composer.ReceivedCalls().Single().GetArguments();
        var intent = (TicketConfirmationIntent)arguments[2]!;
        intent.FirstName.ShouldBe("Alice");
        intent.TicketTypes.ShouldBe(["General Admission"]);
        var delivery = (TicketConfirmationDelivery)arguments[3]!;
        delivery.RecipientAddress.ShouldBe("alice@example.com");
        delivery.IdempotencyKey.ShouldBe(
            $"tickets-changed:{AttendeeTicketsChangedIntegrationEventHandlerFixture.RegistrationGuid}:{fixture.ChangedAt.ToUnixTimeMilliseconds()}");
    }
}
