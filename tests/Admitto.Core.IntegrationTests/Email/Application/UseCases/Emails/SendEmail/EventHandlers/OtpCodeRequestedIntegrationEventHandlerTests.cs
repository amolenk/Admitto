using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

[TestClass]
public sealed class OtpCodeRequestedIntegrationEventHandlerTests(TestContext testContext)
{
    [TestMethod]
    public async ValueTask HandleAsync_EventHasTeamAccentColor_IncludesTeamAccentColorParameter()
    {
        var teamId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var registrationsFacade = Substitute.For<IRegistrationsFacade>();
        registrationsFacade
            .GetEventRegistrationSnapshotAsync(teamId, eventId, Guid.Empty, Arg.Any<CancellationToken>())
            .Returns(new EventRegistrationSnapshotDto(
                Name: "Azure Fest",
                WebsiteUrl: "https://example.com",
                PublicEventLink: "https://tickets.admitto.org/e/azure-fest",
                RegisterLink: "https://tickets.admitto.org/e/azure-fest/register",
                QRCodeLink: "https://tickets.admitto.org/e/azure-fest/qr-code/00000000-0000-0000-0000-000000000000",
                CancelLink: "https://tickets.admitto.org/e/azure-fest/cancel/00000000-0000-0000-0000-000000000000",
                TeamAccentColor: "#0f766e"));
        var sendEmailHandler = new CapturingSendEmailHandler();
        var sut = new OtpCodeRequestedIntegrationEventHandler(registrationsFacade, sendEmailHandler);

        await sut.HandleAsync(new OtpCodeRequestedIntegrationEvent(
            Guid.NewGuid(),
            teamId,
            eventId,
            "Azure Fest",
            "alice@example.com",
            "123456"), testContext.CancellationToken);

        sendEmailHandler.Command.ShouldNotBeNull();
        sendEmailHandler.Command.EmailType.ShouldBe(BuiltInEmailTemplateNames.VerificationCode);
        var teamAccentColor = sendEmailHandler.Command.Parameters.GetType()
            .GetProperty("TeamAccentColor")
            ?.GetValue(sendEmailHandler.Command.Parameters);
        teamAccentColor.ShouldBe("#0f766e");
    }

    private sealed class CapturingSendEmailHandler : ICommandHandler<SendEmailCommand>
    {
        public SendEmailCommand Command { get; private set; } = null!;

        public ValueTask HandleAsync(SendEmailCommand command, CancellationToken cancellationToken)
        {
            Command = command;
            return ValueTask.CompletedTask;
        }
    }
}
