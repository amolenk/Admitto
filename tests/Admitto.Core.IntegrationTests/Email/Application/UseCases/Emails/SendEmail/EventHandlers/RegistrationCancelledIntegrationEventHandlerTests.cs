using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;
using Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
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
        new(TeamGuid.Value, EventGuid.Value, RegId, "alice@example.com", "Alice", "Test", reason);

    private static EventEmailContextDto Context() =>
        new(
            TeamGuid.Value,
            EventGuid.Value,
            "DevConf Team",
            "DevConf 2025",
            "https://devconf.example.com",
            "https://tickets.example.com",
            "https://tickets.example.com/register",
            "https://tickets.example.com/qr-code/" + RegId,
            "https://tickets.example.com/cancel/" + RegId,
            "https://tickets.example.com/edit/" + RegId,
            "Europe/Amsterdam",
            null,
            null,
            null,
            null,
            false);

    private static IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto> ContextQuery()
    {
        var query = Substitute.For<IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto>>();
        query.HandleAsync(
                Arg.Is<GetEventEmailRenderingContextQuery>(q =>
                    q != null && q.TeamId == TeamGuid && q.TicketedEventId == EventGuid),
                Arg.Any<CancellationToken>())
            .Returns(Context());
        return query;
    }

    // Given a registration cancelled event caused by an attendee request
    // When the event is handled
    // Then a cancellation email is sent to the attendee
    [TestMethod]
    public async Task AttendeeRequest_DispatchesCancellationEmail()
    {
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        var sut = new RegistrationCancelledIntegrationEventHandler(ContextQuery(), sendEmailHandler);

        await sut.HandleAsync(Event("AttendeeRequest"), testContext.CancellationToken);

        await sendEmailHandler.Received(1).HandleAsync(
            Arg.Is<SendEmailCommand>(c =>
                c != null &&
                c.EmailType == BuiltInEmailTemplateNames.Cancellation &&
                c.RecipientAddress == "alice@example.com" &&
                c.IdempotencyKey == $"registration-cancelled:{RegId}"),
            Arg.Any<CancellationToken>());
    }

    // Given a registration cancelled event caused by a denied visa letter
    // When the event is handled
    // Then a visa letter denied email is sent to the attendee
    [TestMethod]
    public async Task VisaLetterDenied_DispatchesVisaLetterDeniedEmail()
    {
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        var sut = new RegistrationCancelledIntegrationEventHandler(ContextQuery(), sendEmailHandler);

        await sut.HandleAsync(Event("VisaLetterDenied"), testContext.CancellationToken);

        await sendEmailHandler.Received(1).HandleAsync(
            Arg.Is<SendEmailCommand>(c =>
                c != null &&
                c.EmailType == BuiltInEmailTemplateNames.VisaLetterDenied &&
                c.RecipientAddress == "alice@example.com"),
            Arg.Any<CancellationToken>());
    }

    // Given a registration cancelled event caused by an automatic reconfirm cancellation
    // When the event is handled
    // Then a reconfirm-cancelled email is sent to the attendee
    [TestMethod]
    public async Task ReconfirmAutoCancel_DispatchesReconfirmCancelledEmail()
    {
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        var sut = new RegistrationCancelledIntegrationEventHandler(ContextQuery(), sendEmailHandler);

        await sut.HandleAsync(Event("ReconfirmAutoCancel"), testContext.CancellationToken);

        await sendEmailHandler.Received(1).HandleAsync(
            Arg.Is<SendEmailCommand>(c => c != null && c.EmailType == BuiltInEmailTemplateNames.ReconfirmCancelled),
            Arg.Any<CancellationToken>());
    }

    // Given a registration cancelled event caused by removed ticket types
    // When the event is handled
    // Then no email is sent
    [TestMethod]
    public async Task TicketTypesRemoved_NoEmailDispatched()
    {
        var contextQuery = Substitute.For<IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto>>();
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        var sut = new RegistrationCancelledIntegrationEventHandler(contextQuery, sendEmailHandler);

        await sut.HandleAsync(Event("TicketTypesRemoved"), testContext.CancellationToken);

        await sendEmailHandler.DidNotReceive().HandleAsync(
            Arg.Any<SendEmailCommand>(),
            Arg.Any<CancellationToken>());
    }

    // Given a registration cancelled event caused by an attendee request
    // When the event is handled
    // Then the email recipient name combines the attendee's first and last name
    [TestMethod]
    public async Task AttendeeRequest_PassesFirstNameFromContext()
    {
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        var sut = new RegistrationCancelledIntegrationEventHandler(ContextQuery(), sendEmailHandler);

        await sut.HandleAsync(Event("AttendeeRequest"), testContext.CancellationToken);

        await sendEmailHandler.Received(1).HandleAsync(
            Arg.Is<SendEmailCommand>(c =>
                c != null &&
                c.RecipientName == "Alice Test"),
            Arg.Any<CancellationToken>());
    }

    // Given a registration cancelled event caused by an attendee request
    // When the event is handled
    // Then the email parameters include the event website and register link from the rendering context
    [TestMethod]
    public async Task AttendeeRequest_ParametersIncludeEventWebsite()
    {
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        SendEmailCommand? captured = null;
        sendEmailHandler
            .HandleAsync(Arg.Do<SendEmailCommand>(c => captured = c), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var sut = new RegistrationCancelledIntegrationEventHandler(ContextQuery(), sendEmailHandler);

        await sut.HandleAsync(Event("AttendeeRequest"), testContext.CancellationToken);

        captured.ShouldNotBeNull();

        // Verify the property is named 'EventWebsite' (→ Scriban 'event_website'), not
        // 'EventWebsiteUrl' (→ 'event_website_url') which would leave {{ event_website }} empty.
        var eventWebsite = GetParam(captured.Parameters, "EventWebsite");
        eventWebsite.ShouldBe("https://devconf.example.com");
        GetParam(captured.Parameters, "RegisterLink").ShouldBe("https://tickets.example.com/register");
    }

    private static object? GetParam(object parameters, string name) =>
        parameters.GetType().GetProperty(name)?.GetValue(parameters);
}
