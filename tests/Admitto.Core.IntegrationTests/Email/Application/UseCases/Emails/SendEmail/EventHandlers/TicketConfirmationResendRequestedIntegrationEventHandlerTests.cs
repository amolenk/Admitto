using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTicketConfirmation;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

[TestClass]
public sealed class TicketConfirmationResendRequestedIntegrationEventHandlerTests
{
    // Given a ticket-confirmation resend request
    // When the event is handled
    // Then the common composer receives the resend recipient and exact request key
    [TestMethod]
    public async Task HandleAsync_ResendRequested_DelegatesRecipientAndIntent()
    {
        var fixture = TicketConfirmationResendRequestedIntegrationEventHandlerFixture.Create();

        await new TicketConfirmationResendRequestedIntegrationEventHandler(fixture.Composer)
            .HandleAsync(fixture.IntegrationEvent, CancellationToken.None);

        var arguments = fixture.Composer.ReceivedCalls().Single().GetArguments();
        var intent = (TicketConfirmationIntent)arguments[2]!;
        intent.FirstName.ShouldBe("Alice");
        intent.TicketTypes.ShouldBe(["General Admission"]);
        var delivery = (TicketConfirmationDelivery)arguments[3]!;
        delivery.RecipientAddress.ShouldBe("alice@example.com");
        delivery.IdempotencyKey.ShouldBe(
            $"ticket-confirmation-resend:{TicketConfirmationResendRequestedIntegrationEventHandlerFixture.RegistrationGuid}:{fixture.ResendRequestId}");
    }
}
