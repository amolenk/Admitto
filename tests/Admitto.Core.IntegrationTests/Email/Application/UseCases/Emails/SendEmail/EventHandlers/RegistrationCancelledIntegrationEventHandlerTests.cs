using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using NSubstitute;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

[TestClass]
public sealed class RegistrationCancelledIntegrationEventHandlerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    private static readonly TeamId TeamGuid = TeamId.New();
    private static readonly TicketedEventId EventGuid = TicketedEventId.New();
    private static readonly Guid RegId = Guid.NewGuid();

    private static RegistrationCancelledIntegrationEvent Event(string reason) =>
        new(TeamGuid.Value, EventGuid.Value, RegId, "alice@example.com", reason);

    private static EventRegistrationSnapshotDto Context() =>
        new("DevConf 2025", "https://devconf.example.com", "https://tickets.example.com", "https://devconf.example.com/qr", "https://tickets.example.com/cancel", "Alice", "Test");

    [TestMethod]
    public async Task AttendeeRequest_DispatchesCancellationEmail()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetEventRegistrationSnapshotAsync(TeamGuid.Value, EventGuid.Value, RegId, Arg.Any<CancellationToken>())
            .Returns(Context());
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        var sut = new RegistrationCancelledIntegrationEventHandler(facade, sendEmailHandler);

        await sut.HandleAsync(Event("AttendeeRequest"), testContext.CancellationToken);

        await sendEmailHandler.Received(1).HandleAsync(
            Arg.Is<SendEmailCommand>(c =>
                c.EmailType == BuiltInEmailTemplateNames.Cancellation &&
                c.RecipientAddress == "alice@example.com" &&
                c.IdempotencyKey == $"registration-cancelled:{RegId}"),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task VisaLetterDenied_DispatchesVisaLetterDeniedEmail()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetEventRegistrationSnapshotAsync(TeamGuid.Value, EventGuid.Value, RegId, Arg.Any<CancellationToken>())
            .Returns(Context());
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        var sut = new RegistrationCancelledIntegrationEventHandler(facade, sendEmailHandler);

        await sut.HandleAsync(Event("VisaLetterDenied"), testContext.CancellationToken);

        await sendEmailHandler.Received(1).HandleAsync(
            Arg.Is<SendEmailCommand>(c =>
                c.EmailType == BuiltInEmailTemplateNames.VisaLetterDenied &&
                c.RecipientAddress == "alice@example.com"),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task ReconfirmAutoCancel_DispatchesReconfirmCancelledEmail()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetEventRegistrationSnapshotAsync(TeamGuid.Value, EventGuid.Value, RegId, Arg.Any<CancellationToken>())
            .Returns(Context());
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        var sut = new RegistrationCancelledIntegrationEventHandler(facade, sendEmailHandler);

        await sut.HandleAsync(Event("ReconfirmAutoCancel"), testContext.CancellationToken);

        await sendEmailHandler.Received(1).HandleAsync(
            Arg.Is<SendEmailCommand>(c => c.EmailType == BuiltInEmailTemplateNames.ReconfirmCancelled),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task TicketTypesRemoved_NoEmailDispatched()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        var sut = new RegistrationCancelledIntegrationEventHandler(facade, sendEmailHandler);

        await sut.HandleAsync(Event("TicketTypesRemoved"), testContext.CancellationToken);

        await facade.DidNotReceive().GetEventRegistrationSnapshotAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await sendEmailHandler.DidNotReceive().HandleAsync(
            Arg.Any<SendEmailCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task AttendeeRequest_PassesFirstNameFromContext()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetEventRegistrationSnapshotAsync(TeamGuid.Value, EventGuid.Value, RegId, Arg.Any<CancellationToken>())
            .Returns(Context());
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        var sut = new RegistrationCancelledIntegrationEventHandler(facade, sendEmailHandler);

        await sut.HandleAsync(Event("AttendeeRequest"), testContext.CancellationToken);

        await sendEmailHandler.Received(1).HandleAsync(
            Arg.Is<SendEmailCommand>(c =>
                c.RecipientName == "Alice Test"),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task AttendeeRequest_ParametersIncludeEventWebsite()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetEventRegistrationSnapshotAsync(TeamGuid.Value, EventGuid.Value, RegId, Arg.Any<CancellationToken>())
            .Returns(Context());
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        SendEmailCommand? captured = null;
        sendEmailHandler
            .HandleAsync(Arg.Do<SendEmailCommand>(c => captured = c), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var sut = new RegistrationCancelledIntegrationEventHandler(facade, sendEmailHandler);

        await sut.HandleAsync(Event("AttendeeRequest"), testContext.CancellationToken);

        captured.ShouldNotBeNull();

        // Verify the property is named 'EventWebsite' (→ Scriban 'event_website'), not
        // 'EventWebsiteUrl' (→ 'event_website_url') which would leave {{ event_website }} empty.
        var eventWebsite = GetParam(captured.Parameters, "EventWebsite");
        eventWebsite.ShouldBe("https://devconf.example.com");
        GetParam(captured.Parameters, "RegisterLink").ShouldBe("https://tickets.example.com");
    }

    private static object? GetParam(object parameters, string name) =>
        parameters.GetType().GetProperty(name)?.GetValue(parameters);
}
