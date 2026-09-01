using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTransactionalEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

[TestClass]
public sealed class OtpCodeRequestedIntegrationEventHandlerTests(TestContext testContext)
{
    // Given an attendee has requested a verification code
    // When the verification-code event is processed
    // Then the attendee receives an email containing the verification code
    [TestMethod]
    public async ValueTask HandleAsync_OtpCodeRequested_SendsVerificationCodeEmail()
    {
        var teamId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var otpCodeId = Guid.NewGuid();
        var composer = Substitute.For<ITransactionalEmailComposer>();
        composer.ReturnRenderedEmail(BuiltInEmailTemplateNames.VerificationCode);
        var deliveryHandler = Substitute.For<ICommandHandler<PrepareEmailDeliveryCommand>>();
        var sut = new OtpCodeRequestedIntegrationEventHandler(composer, deliveryHandler);

        await sut.HandleAsync(new OtpCodeRequestedIntegrationEvent(
            otpCodeId,
            teamId,
            eventId,
            "Azure Fest",
            "alice@example.com",
            "123456"), testContext.CancellationToken);

        await composer.Received(1).ComposeAsync(
            Arg.Is<VerificationCodeIntent>(intent =>
                intent!.TeamId == TeamId.From(teamId)
                && intent.TicketedEventId == TicketedEventId.From(eventId)
                && intent.PlainCode == "123456"),
            Arg.Any<CancellationToken>());

        var delivery = deliveryHandler.ReceivedDelivery();
        delivery.RecipientAddress.ShouldBe("alice@example.com");
        delivery.IdempotencyKey.ShouldBe($"otp-requested:{otpCodeId}");
        delivery.EmailType.ShouldBe(BuiltInEmailTemplateNames.VerificationCode);
        delivery.RegistrationId.ShouldBeNull();
    }
}
