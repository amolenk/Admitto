using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.SendEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.SendEmail.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using NSubstitute;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.SendEmail.EventHandlers;

[TestClass]
public sealed class AttendeeRegisteredIntegrationEventHandlerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    private static readonly TeamId TeamGuid = TeamId.New();
    private static readonly TicketedEventId EventGuid = TicketedEventId.New();
    private static readonly Guid RegId = Guid.NewGuid();

    private static AttendeeRegisteredIntegrationEvent Event() =>
        new(TeamGuid.Value, EventGuid.Value, RegId, "alice@example.com", "Alice", "Anderson", []);

    private static TicketedEventEmailContextDto Context() =>
        new("DevConf 2025", "https://devconf.example.com", "https://tickets.example.com", "https://devconf.example.com/qr", "Alice", "Anderson");

    [TestMethod]
    public async Task AttendeeRegistered_DispatchesTicketEmail()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetTicketedEventEmailContextAsync(EventGuid.Value, RegId, Arg.Any<CancellationToken>())
            .Returns(Context());
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        var sut = new AttendeeRegisteredIntegrationEventHandler(facade, sendEmailHandler);

        await sut.HandleAsync(Event(), testContext.CancellationToken);

        await sendEmailHandler.Received(1).HandleAsync(
            Arg.Is<SendEmailCommand>(c =>
                c.EmailType == BuiltInEmailTemplateNames.TicketConfirmation &&
                c.RecipientAddress == "alice@example.com" &&
                c.IdempotencyKey == $"attendee-registered:{RegId}"),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task AttendeeRegistered_ParametersIncludeEventWebsite()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetTicketedEventEmailContextAsync(EventGuid.Value, RegId, Arg.Any<CancellationToken>())
            .Returns(Context());
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        SendEmailCommand? captured = null;
        sendEmailHandler
            .HandleAsync(Arg.Do<SendEmailCommand>(c => captured = c), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var sut = new AttendeeRegisteredIntegrationEventHandler(facade, sendEmailHandler);

        await sut.HandleAsync(Event(), testContext.CancellationToken);

        captured.ShouldNotBeNull();

        // Verify the property is named 'EventWebsite' (→ Scriban 'event_website'), not
        // 'EventWebsiteUrl' (→ 'event_website_url') which would leave {{ event_website }} empty.
        var eventWebsite = GetParam(captured.Parameters, "EventWebsite");
        eventWebsite.ShouldBe("https://devconf.example.com");
    }

    private static object? GetParam(object parameters, string name) =>
        parameters.GetType().GetProperty(name)?.GetValue(parameters);
}
