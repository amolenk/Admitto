using Amolenk.Admitto.Module.Email.Application.UseCases.SendEmail;
using Amolenk.Admitto.Module.Email.Application.UseCases.SendEmail.EventHandlers;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Email.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using NSubstitute;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Tests.Application.UseCases.SendEmail.EventHandlers;

[TestClass]
public sealed class AttendeeRegisteredIntegrationEventHandlerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    private static readonly Guid TeamId = Guid.NewGuid();
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly Guid RegId = Guid.NewGuid();

    private static AttendeeRegisteredIntegrationEvent Event() =>
        new(TeamId, EventId, RegId, "alice@example.com", "Alice", "Anderson", []);

    private static TicketedEventEmailContextDto Context() =>
        new("DevConf 2025", "https://devconf.example.com", "https://devconf.example.com/qr", "Alice", "Anderson");

    [TestMethod]
    public async Task SC001_AttendeeRegistered_DispatchesTicketEmail()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetTicketedEventEmailContextAsync(EventId, RegId, Arg.Any<CancellationToken>())
            .Returns(Context());
        var mediator = Substitute.For<IMediator>();

        var sut = new AttendeeRegisteredIntegrationEventHandler(
            Environment.Database.Context, facade, mediator);

        await sut.HandleAsync(Event(), testContext.CancellationToken);

        await mediator.Received(1).SendAsync(
            Arg.Is<SendEmailCommand>(c =>
                c.EmailType == EmailTemplateType.Ticket &&
                c.RecipientAddress == "alice@example.com" &&
                c.IdempotencyKey == $"attendee-registered:{RegId}"),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SC002_AttendeeRegistered_ParametersIncludeEventWebsite()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetTicketedEventEmailContextAsync(EventId, RegId, Arg.Any<CancellationToken>())
            .Returns(Context());
        var mediator = Substitute.For<IMediator>();

        SendEmailCommand? captured = null;
        await mediator.SendAsync(
            Arg.Do<SendEmailCommand>(c => captured = c),
            Arg.Any<CancellationToken>());

        var sut = new AttendeeRegisteredIntegrationEventHandler(
            Environment.Database.Context, facade, mediator);

        await sut.HandleAsync(Event(), testContext.CancellationToken);

        captured.ShouldNotBeNull();

        // Verify the property is named 'EventWebsite' (→ Scriban 'event_website'), not
        // 'EventWebsiteUrl' (→ 'event_website_url') which would leave {{ event_website }} empty.
        var eventWebsite = GetParam(captured.Parameters, "EventWebsite");
        eventWebsite.ShouldBe("https://devconf.example.com");
    }

    [TestMethod]
    public async Task SC003_AlreadyHandled_SkipsDispatch()
    {
        var idempotencyKey = $"attendee-registered:{RegId}";
        await Environment.Database.SeedAsync(db =>
        {
            var log = EmailLog.Create(
                TeamId, EventId, idempotencyKey,
                "alice@example.com", EmailTemplateType.Ticket,
                "Subject", "smtp", null, EmailLogStatus.Sent,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            db.EmailLog.Add(log);
        });

        var facade = Substitute.For<IRegistrationsFacade>();
        var mediator = Substitute.For<IMediator>();

        var sut = new AttendeeRegisteredIntegrationEventHandler(
            Environment.Database.Context, facade, mediator);

        await sut.HandleAsync(Event(), testContext.CancellationToken);

        await mediator.DidNotReceive().SendAsync(
            Arg.Any<SendEmailCommand>(), Arg.Any<CancellationToken>());
    }

    private static object? GetParam(object parameters, string name) =>
        parameters.GetType().GetProperty(name)?.GetValue(parameters);
}
