using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;
using Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

[TestClass]
public sealed class OtpCodeRequestedIntegrationEventHandlerTests(TestContext testContext)
{
    // Given an OTP code was requested for an event
    // When the OTP code requested integration event is handled
    // Then a verification-code email is sent without an accent color smuggled through the parameters
    [TestMethod]
    public async ValueTask HandleAsync_OtpCodeRequested_LeavesBrandingToEffectiveEmailSettings()
    {
        var teamId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var contextQuery = Substitute.For<IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto>>();
        contextQuery
            .HandleAsync(
                Arg.Is<GetEventEmailRenderingContextQuery>(q =>
                    q != null &&
                    q.TeamId == TeamId.From(teamId)
                    && q.TicketedEventId == TicketedEventId.From(eventId)
                    && q.RegistrationId == null),
                Arg.Any<CancellationToken>())
            .Returns(new EventEmailContextDto(
                teamId,
                eventId,
                "DevConf Team",
                "Azure Fest",
                "https://example.com",
                "https://tickets.admitto.org/e/azure-fest",
                "https://tickets.admitto.org/e/azure-fest/register",
                "https://tickets.admitto.org/e/azure-fest/qr-code/00000000-0000-0000-0000-000000000000",
                "https://tickets.admitto.org/e/azure-fest/cancel/00000000-0000-0000-0000-000000000000",
                "https://tickets.admitto.org/e/azure-fest/edit/00000000-0000-0000-0000-000000000000",
                "UTC",
                null,
                null,
                null,
                false));
        var sendEmailHandler = new CapturingSendEmailHandler();
        var sut = new OtpCodeRequestedIntegrationEventHandler(contextQuery, sendEmailHandler);

        await sut.HandleAsync(new OtpCodeRequestedIntegrationEvent(
            Guid.NewGuid(),
            teamId,
            eventId,
            "Azure Fest",
            "alice@example.com",
            "123456"), testContext.CancellationToken);

        sendEmailHandler.Command.ShouldNotBeNull();
        sendEmailHandler.Command.EmailType.ShouldBe(BuiltInEmailTemplateNames.VerificationCode);

        // Branding is resolved once at send time from EffectiveEmailSettings, so the
        // event handler must not smuggle an accent color through the parameters object.
        sendEmailHandler.Command.Parameters.GetType()
            .GetProperty("AccentColor")
            .ShouldBeNull();
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
