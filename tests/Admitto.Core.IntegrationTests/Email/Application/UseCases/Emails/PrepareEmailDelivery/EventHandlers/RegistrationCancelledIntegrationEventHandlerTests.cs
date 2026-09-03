using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;

[TestClass]
public sealed class RegistrationCancelledIntegrationEventHandlerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    private static readonly TeamId TeamGuid = TeamId.New();
    private static readonly TicketedEventId EventGuid = TicketedEventId.New();
    private static readonly Guid RegistrationGuid = Guid.NewGuid();

    private static RegistrationCancelledIntegrationEvent Event(string reason) =>
        new(TeamGuid.Value, EventGuid.Value, RegistrationGuid, "alice@example.com", "Alice", "Test", reason)
        {
            IntegrationEventId = Guid.Parse("11111111-1111-1111-1111-111111111111")
        };

    // Given an attendee has asked to cancel their registration
    // When the attendee-cancellation event is processed
    // Then the attendee receives a cancellation email
    [TestMethod]
    public async Task HandleAsync_AttendeeRequest_UsesTypedIntentAndDelivery()
    {
        var composer = Substitute.For<ITransactionalEmailComposer>();
        composer.ReturnRenderedEmail(BuiltInEmailTemplateNames.Cancellation);
        var deliveryHandler = Substitute.For<ICommandHandler<PrepareEmailDeliveryCommand>>();
        var sut = new RegistrationCancelledIntegrationEventHandler(composer, deliveryHandler);

        await sut.HandleAsync(Event("AttendeeRequest"), testContext.CancellationToken);

        await composer.Received(1).ComposeAsync(
            Arg.Is<AttendeeRequestCancellationIntent>(intent =>
                intent != null && intent.TeamId == TeamGuid && intent.TicketedEventId == EventGuid
                && intent.FirstName == "Alice" && intent.RegistrationId.Value == RegistrationGuid),
            Arg.Any<CancellationToken>());

        var delivery = deliveryHandler.ReceivedDelivery();
        delivery.RecipientAddress.ShouldBe("alice@example.com");
        delivery.RecipientName.ShouldBe("Alice Test");
        delivery.IdempotencyKey.ShouldBe("registration-cancelled:11111111111111111111111111111111");
        delivery.EmailType.ShouldBe(BuiltInEmailTemplateNames.Cancellation);
        delivery.RegistrationId.ShouldBe(RegistrationGuid);
    }

    // Given an attendee's visa letter request has been denied
    // When the visa-letter-denied cancellation event is processed
    // Then the attendee receives a visa-denial cancellation email
    [TestMethod]
    public async Task HandleAsync_VisaLetterDenied_UsesTypedIntent()
    {
        var composer = Substitute.For<ITransactionalEmailComposer>();
        composer.ReturnRenderedEmail(BuiltInEmailTemplateNames.VisaLetterDenied);
        var deliveryHandler = Substitute.For<ICommandHandler<PrepareEmailDeliveryCommand>>();
        var sut = new RegistrationCancelledIntegrationEventHandler(composer, deliveryHandler);

        await sut.HandleAsync(Event("VisaLetterDenied"), testContext.CancellationToken);

        await composer.Received(1).ComposeAsync(
            Arg.Is<VisaLetterDeniedCancellationIntent>(intent => intent != null && intent.FirstName == "Alice"),
            Arg.Any<CancellationToken>());

        var delivery = deliveryHandler.ReceivedDelivery();
        delivery.RecipientAddress.ShouldBe("alice@example.com");
        delivery.IdempotencyKey.ShouldBe("registration-cancelled:11111111111111111111111111111111");
        delivery.EmailType.ShouldBe(BuiltInEmailTemplateNames.VisaLetterDenied);
        delivery.RegistrationId.ShouldBe(RegistrationGuid);
    }

    // Given an attendee has not reconfirmed in time
    // When the reconfirm auto-cancellation event is processed
    // Then the attendee receives a reconfirmation cancellation email
    [TestMethod]
    public async Task HandleAsync_ReconfirmAutoCancel_UsesTypedIntent()
    {
        var composer = Substitute.For<ITransactionalEmailComposer>();
        composer.ReturnRenderedEmail(BuiltInEmailTemplateNames.ReconfirmCancelled);
        var deliveryHandler = Substitute.For<ICommandHandler<PrepareEmailDeliveryCommand>>();
        var sut = new RegistrationCancelledIntegrationEventHandler(composer, deliveryHandler);

        await sut.HandleAsync(Event("ReconfirmAutoCancel"), testContext.CancellationToken);

        await composer.Received(1).ComposeAsync(
            Arg.Is<ReconfirmAutoCancellationIntent>(intent => intent != null && intent.FirstName == "Alice"),
            Arg.Any<CancellationToken>());

        var delivery = deliveryHandler.ReceivedDelivery();
        delivery.RecipientAddress.ShouldBe("alice@example.com");
        delivery.IdempotencyKey.ShouldBe("registration-cancelled:11111111111111111111111111111111");
        delivery.EmailType.ShouldBe(BuiltInEmailTemplateNames.ReconfirmCancelled);
        delivery.RegistrationId.ShouldBe(RegistrationGuid);
    }

    // Given the same cancellation arrives twice as separate events
    // When both cancellation events are processed
    // Then both events produce separate cancellation emails
    [TestMethod]
    public async Task HandleAsync_DifferentIntegrationMessageIds_UsesEachForIdempotency()
    {
        var composer = Substitute.For<ITransactionalEmailComposer>();
        composer.ReturnRenderedEmail();
        var deliveryHandler = Substitute.For<ICommandHandler<PrepareEmailDeliveryCommand>>();
        var second = Event("ReconfirmAutoCancel") with
        {
            IntegrationEventId = Guid.Parse("22222222-2222-2222-2222-222222222222")
        };
        var sut = new RegistrationCancelledIntegrationEventHandler(composer, deliveryHandler);

        await sut.HandleAsync(Event("ReconfirmAutoCancel"), testContext.CancellationToken);
        await sut.HandleAsync(second, testContext.CancellationToken);

        var deliveries = composer.ReceivedCalls()
            .Select(call => call.GetArguments()[0])
            .OfType<ReconfirmAutoCancellationIntent>()
            .ToList();
        deliveries.Count.ShouldBe(2);
        deliveries[0].RegistrationId.Value.ShouldBe(RegistrationGuid);
        deliveries[1].RegistrationId.Value.ShouldBe(RegistrationGuid);
        var deliveryKeys = deliveryHandler.ReceivedCalls()
            .Select(call => ((PrepareEmailDeliveryCommand)call.GetArguments()[0]!).IdempotencyKey)
            .ToList();
        deliveryKeys.ShouldBe([
            "registration-cancelled:11111111111111111111111111111111",
            "registration-cancelled:22222222222222222222222222222222"]);
    }

    // Given an attendee's registration was cancelled because tickets were removed
    // When the ticket-removal cancellation event is processed
    // Then no cancellation email is sent
    [TestMethod]
    public async Task HandleAsync_TicketTypesRemoved_SilentlySkipsCancellationEmail()
    {
        var composer = Substitute.For<ITransactionalEmailComposer>();
        composer.ReturnRenderedEmail();
        var deliveryHandler = Substitute.For<ICommandHandler<PrepareEmailDeliveryCommand>>();
        var sut = new RegistrationCancelledIntegrationEventHandler(composer, deliveryHandler);

        await sut.HandleAsync(Event("TicketTypesRemoved"), testContext.CancellationToken);

        await composer.DidNotReceiveWithAnyArgs().ComposeAsync(default!, default);
        await deliveryHandler.DidNotReceiveWithAnyArgs().HandleAsync(default!, default);
    }
}
