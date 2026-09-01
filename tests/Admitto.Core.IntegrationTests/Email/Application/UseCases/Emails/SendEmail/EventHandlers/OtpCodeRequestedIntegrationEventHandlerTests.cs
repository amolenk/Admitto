using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeVerificationCode;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

[TestClass]
public sealed class OtpCodeRequestedIntegrationEventHandlerTests(TestContext testContext)
{
    // Given an OTP code was requested for an event
    // When the OTP code requested integration event is handled
    // Then the typed composer receives only the code, recipient, and idempotency data
    [TestMethod]
    public async ValueTask HandleAsync_OtpCodeRequested_TranslatesToTypedComposer()
    {
        var teamId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var otpCodeId = Guid.NewGuid();
        var composer = Substitute.For<IVerificationCodeEmailComposer>();
        var sut = new OtpCodeRequestedIntegrationEventHandler(composer);

        await sut.HandleAsync(new OtpCodeRequestedIntegrationEvent(
            otpCodeId,
            teamId,
            eventId,
            "Azure Fest",
            "alice@example.com",
            "123456"), testContext.CancellationToken);

        await composer.Received(1).ComposeAsync(
            TeamId.From(teamId),
            TicketedEventId.From(eventId),
            Arg.Is<VerificationCodeIntent>(intent => intent!.PlainCode == "123456"),
            Arg.Is<VerificationCodeDelivery>(delivery =>
                delivery!.RecipientAddress == "alice@example.com"
                && delivery.RecipientName == "alice@example.com"
                && delivery.IdempotencyKey == $"otp-requested:{otpCodeId}"),
            Arg.Any<CancellationToken>());
    }
}
