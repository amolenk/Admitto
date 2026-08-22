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
public sealed class AttendeeRegisteredIntegrationEventHandlerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    private static readonly TeamId TeamGuid = TeamId.New();
    private static readonly TicketedEventId EventGuid = TicketedEventId.New();
    private static readonly Guid RegId = Guid.NewGuid();

    private static AttendeeRegisteredIntegrationEvent Event() =>
        new(
            TeamGuid.Value,
            EventGuid.Value,
            RegId,
            "alice@example.com",
            "Alice",
            "Anderson",
            [],
            DateTimeOffset.UtcNow);

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

    // Given an AttendeeRegistered integration event for an attendee
    // When the event is handled
    // Then a ticket confirmation email is sent to the attendee with an idempotency key derived from the registration
    [TestMethod]
    public async Task AttendeeRegistered_DispatchesTicketEmail()
    {
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        var sut = new AttendeeRegisteredIntegrationEventHandler(ContextQuery(), sendEmailHandler);

        var evt = Event();
        await sut.HandleAsync(evt, testContext.CancellationToken);

        await sendEmailHandler.Received(1).HandleAsync(
            Arg.Is<SendEmailCommand>(c =>
                c != null &&
                c.EmailType == BuiltInEmailTemplateNames.TicketConfirmation &&
                c.RecipientAddress == "alice@example.com" &&
                c.IdempotencyKey == $"attendee-registered:{RegId}:{evt.RegisteredAt:O}"),
            Arg.Any<CancellationToken>());
    }

    // Given an AttendeeRegistered integration event for an attendee
    // When the event is handled
    // Then the sent email's parameters include the event website under the 'EventWebsite' property name
    [TestMethod]
    public async Task AttendeeRegistered_ParametersIncludeEventWebsite()
    {
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        SendEmailCommand? captured = null;
        sendEmailHandler
            .HandleAsync(Arg.Do<SendEmailCommand>(c => captured = c), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var sut = new AttendeeRegisteredIntegrationEventHandler(ContextQuery(), sendEmailHandler);

        await sut.HandleAsync(Event(), testContext.CancellationToken);

        captured.ShouldNotBeNull();

        // Verify the property is named 'EventWebsite' (→ Scriban 'event_website'), not
        // 'EventWebsiteUrl' (→ 'event_website_url') which would leave {{ event_website }} empty.
        var eventWebsite = GetParam(captured.Parameters, "EventWebsite");
        eventWebsite.ShouldBe("https://devconf.example.com");
    }

    // Given an AttendeeRegistered integration event for an attendee
    // When the event is handled
    // Then the sent email's parameters include the edit-registration link for that attendee
    [TestMethod]
    public async Task AttendeeRegistered_ParametersIncludeEditRegistrationLink()
    {
        var sendEmailHandler = Substitute.For<ICommandHandler<SendEmailCommand>>();

        SendEmailCommand? captured = null;
        sendEmailHandler
            .HandleAsync(Arg.Do<SendEmailCommand>(c => captured = c), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);

        var sut = new AttendeeRegisteredIntegrationEventHandler(ContextQuery(), sendEmailHandler);

        await sut.HandleAsync(Event(), testContext.CancellationToken);

        captured.ShouldNotBeNull();
        GetParam(captured.Parameters, "EditRegistrationLink")
            .ShouldBe("https://tickets.example.com/edit/" + RegId);
    }

    private static object? GetParam(object parameters, string name) =>
        parameters.GetType().GetProperty(name)?.GetValue(parameters);
}
