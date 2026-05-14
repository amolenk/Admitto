using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.SendEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.SendEmail.EventHandlers;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using NSubstitute;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.SendEmail.EventHandlers;

[TestClass]
public sealed class RegistrationCancelledIntegrationEventHandlerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    private static readonly TeamId TeamGuid = TeamId.New();
    private static readonly TicketedEventId EventGuid = TicketedEventId.New();
    private static readonly Guid RegId = Guid.NewGuid();

    private static RegistrationCancelledIntegrationEvent Event(string reason) =>
        new(TeamGuid.Value, EventGuid.Value, RegId, "alice@example.com", reason);

    private static TicketedEventEmailContextDto Context() =>
        new("DevConf 2025", "https://devconf.example.com", "https://devconf.example.com/qr", "Alice", "Test");

    [TestMethod]
    public async Task AttendeeRequest_DispatchesCancellationEmail()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetTicketedEventEmailContextAsync(EventGuid.Value, RegId, Arg.Any<CancellationToken>())
            .Returns(Context());
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        var sut = new RegistrationCancelledIntegrationEventHandler(
            Environment.EmailDatabase.Context, facade, sendEmailHandler);

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
        facade.GetTicketedEventEmailContextAsync(EventGuid.Value, RegId, Arg.Any<CancellationToken>())
            .Returns(Context());
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        var sut = new RegistrationCancelledIntegrationEventHandler(
            Environment.EmailDatabase.Context, facade, sendEmailHandler);

        await sut.HandleAsync(Event("VisaLetterDenied"), testContext.CancellationToken);

        await sendEmailHandler.Received(1).HandleAsync(
            Arg.Is<SendEmailCommand>(c =>
                c.EmailType == BuiltInEmailTemplateNames.VisaLetterDenied &&
                c.RecipientAddress == "alice@example.com"),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task TicketTypesRemoved_NoEmailDispatched()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        var sut = new RegistrationCancelledIntegrationEventHandler(
            Environment.EmailDatabase.Context, facade, sendEmailHandler);

        await sut.HandleAsync(Event("TicketTypesRemoved"), testContext.CancellationToken);

        await facade.DidNotReceive().GetTicketedEventEmailContextAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await sendEmailHandler.DidNotReceive().HandleAsync(
            Arg.Any<SendEmailCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task AlreadyHandled_SkipsDispatch()
    {
        var idempotencyKey = $"registration-cancelled:{RegId}";
        await Environment.EmailDatabase.SeedAsync(db =>
        {
            var log = EmailLog.Create(
                TeamGuid, EventGuid, idempotencyKey,
                EmailAddress.From("alice@example.com"), BuiltInEmailTemplateNames.Cancellation,
                "Subject", "smtp", null, EmailLogStatus.Sent,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            db.EmailLog.Add(log);
        });

        var facade = Substitute.For<IRegistrationsFacade>();
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        var sut = new RegistrationCancelledIntegrationEventHandler(
            Environment.EmailDatabase.Context, facade, sendEmailHandler);

        await sut.HandleAsync(Event("AttendeeRequest"), testContext.CancellationToken);

        await sendEmailHandler.DidNotReceive().HandleAsync(
            Arg.Any<SendEmailCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task AttendeeRequest_PassesFirstNameFromContext()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetTicketedEventEmailContextAsync(EventGuid.Value, RegId, Arg.Any<CancellationToken>())
            .Returns(Context());
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        var sut = new RegistrationCancelledIntegrationEventHandler(
            Environment.EmailDatabase.Context, facade, sendEmailHandler);

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
        facade.GetTicketedEventEmailContextAsync(EventGuid.Value, RegId, Arg.Any<CancellationToken>())
            .Returns(Context());
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        SendEmailCommand? captured = null;
        sendEmailHandler
            .HandleAsync(Arg.Do<SendEmailCommand>(c => captured = c), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var sut = new RegistrationCancelledIntegrationEventHandler(
            Environment.EmailDatabase.Context, facade, sendEmailHandler);

        await sut.HandleAsync(Event("AttendeeRequest"), testContext.CancellationToken);

        captured.ShouldNotBeNull();

        // Verify the property is named 'EventWebsite' (→ Scriban 'event_website'), not
        // 'EventWebsiteUrl' (→ 'event_website_url') which would leave {{ event_website }} empty.
        var eventWebsite = GetParam(captured.Parameters, "EventWebsite");
        eventWebsite.ShouldBe("https://devconf.example.com");
    }

    private static object? GetParam(object parameters, string name) =>
        parameters.GetType().GetProperty(name)?.GetValue(parameters);
}
