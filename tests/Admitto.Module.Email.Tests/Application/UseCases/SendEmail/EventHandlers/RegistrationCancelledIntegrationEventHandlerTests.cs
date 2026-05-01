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
public sealed class RegistrationCancelledIntegrationEventHandlerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    private static readonly Guid TeamId = Guid.NewGuid();
    private static readonly Guid EventId = Guid.NewGuid();
    private static readonly Guid RegId = Guid.NewGuid();

    private static RegistrationCancelledIntegrationEvent Event(string reason) =>
        new(TeamId, EventId, RegId, "alice@example.com", reason);

    private static TicketedEventEmailContextDto Context() =>
        new("DevConf 2025", "https://devconf.example.com", "https://devconf.example.com/qr", "Alice", "Test");

    [TestMethod]
    public async Task SC001_AttendeeRequest_DispatchesCancellationEmail()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetTicketedEventEmailContextAsync(EventId, RegId, Arg.Any<CancellationToken>())
            .Returns(Context());
        var mediator = Substitute.For<IMediator>();

        var sut = new RegistrationCancelledIntegrationEventHandler(
            Environment.Database.Context, facade, mediator);

        await sut.HandleAsync(Event("AttendeeRequest"), testContext.CancellationToken);

        await mediator.Received(1).SendAsync(
            Arg.Is<SendEmailCommand>(c =>
                c.EmailType == EmailTemplateType.Cancellation &&
                c.RecipientAddress == "alice@example.com" &&
                c.IdempotencyKey == $"registration-cancelled:{RegId}"),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SC002_VisaLetterDenied_DispatchesVisaLetterDeniedEmail()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetTicketedEventEmailContextAsync(EventId, RegId, Arg.Any<CancellationToken>())
            .Returns(Context());
        var mediator = Substitute.For<IMediator>();

        var sut = new RegistrationCancelledIntegrationEventHandler(
            Environment.Database.Context, facade, mediator);

        await sut.HandleAsync(Event("VisaLetterDenied"), testContext.CancellationToken);

        await mediator.Received(1).SendAsync(
            Arg.Is<SendEmailCommand>(c =>
                c.EmailType == EmailTemplateType.VisaLetterDenied &&
                c.RecipientAddress == "alice@example.com"),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SC003_TicketTypesRemoved_NoEmailDispatched()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        var mediator = Substitute.For<IMediator>();

        var sut = new RegistrationCancelledIntegrationEventHandler(
            Environment.Database.Context, facade, mediator);

        await sut.HandleAsync(Event("TicketTypesRemoved"), testContext.CancellationToken);

        await facade.DidNotReceive().GetTicketedEventEmailContextAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await mediator.DidNotReceive().SendAsync(
            Arg.Any<SendEmailCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SC004_AlreadyHandled_SkipsDispatch()
    {
        var idempotencyKey = $"registration-cancelled:{RegId}";
        await Environment.Database.SeedAsync(db =>
        {
            var log = EmailLog.Create(
                TeamId, EventId, idempotencyKey,
                "alice@example.com", EmailTemplateType.Cancellation,
                "Subject", "smtp", null, EmailLogStatus.Sent,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            db.EmailLog.Add(log);
        });

        var facade = Substitute.For<IRegistrationsFacade>();
        var mediator = Substitute.For<IMediator>();

        var sut = new RegistrationCancelledIntegrationEventHandler(
            Environment.Database.Context, facade, mediator);

        await sut.HandleAsync(Event("AttendeeRequest"), testContext.CancellationToken);

        await mediator.DidNotReceive().SendAsync(
            Arg.Any<SendEmailCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SC005_AttendeeRequest_PassesFirstNameFromContext()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetTicketedEventEmailContextAsync(EventId, RegId, Arg.Any<CancellationToken>())
            .Returns(Context());
        var mediator = Substitute.For<IMediator>();

        var sut = new RegistrationCancelledIntegrationEventHandler(
            Environment.Database.Context, facade, mediator);

        await sut.HandleAsync(Event("AttendeeRequest"), testContext.CancellationToken);

        await mediator.Received(1).SendAsync(
            Arg.Is<SendEmailCommand>(c =>
                c.RecipientName == "Alice Test"),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SC006_AttendeeRequest_ParametersIncludeEventWebsite()
    {
        var facade = Substitute.For<IRegistrationsFacade>();
        facade.GetTicketedEventEmailContextAsync(EventId, RegId, Arg.Any<CancellationToken>())
            .Returns(Context());
        var mediator = Substitute.For<IMediator>();

        SendEmailCommand? captured = null;
        await mediator.SendAsync(
            Arg.Do<SendEmailCommand>(c => captured = c),
            Arg.Any<CancellationToken>());

        var sut = new RegistrationCancelledIntegrationEventHandler(
            Environment.Database.Context, facade, mediator);

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
