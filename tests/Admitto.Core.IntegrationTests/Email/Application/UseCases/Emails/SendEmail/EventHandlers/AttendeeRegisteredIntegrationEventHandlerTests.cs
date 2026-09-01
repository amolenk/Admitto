using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTransactionalEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
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

    // Given an attendee has completed registration
    // When the registration confirmation event is processed
    // Then the attendee receives a ticket confirmation email
    [TestMethod]
    public async Task AttendeeRegistered_DispatchesTicketEmail()
    {
        var composer = Substitute.For<ITransactionalEmailComposer>();
        composer.ReturnRenderedEmail();

        var deliveryHandler = Substitute.For<ICommandHandler<PrepareEmailDeliveryCommand>>();
        var sut = new AttendeeRegisteredIntegrationEventHandler(composer, deliveryHandler);

        var evt = Event();
        await sut.HandleAsync(evt, testContext.CancellationToken);

        await composer.Received(1).ComposeAsync(
            Arg.Is<TicketConfirmationIntent>(d =>
                d!.TeamId == TeamGuid && d.TicketedEventId == EventGuid &&
                d.RegistrationId == RegistrationId.From(RegId)),
            Arg.Any<CancellationToken>());

        var delivery = deliveryHandler.ReceivedDelivery();
        delivery.RecipientAddress.ShouldBe("alice@example.com");
        delivery.RecipientName.ShouldBe("Alice Anderson");
        delivery.IdempotencyKey.ShouldBe($"attendee-registered:{RegId}:{evt.RegisteredAt:O}");
        delivery.EmailType.ShouldBe(BuiltInEmailTemplateNames.TicketConfirmation);
        delivery.RegistrationId.ShouldBe(RegId);
    }

    // Given an attendee has completed registration
    // When the registration confirmation event is processed
    // Then the ticket confirmation includes the attendee's name
    [TestMethod]
    public async Task AttendeeRegistered_IntentIncludesAttendeeFacts()
    {
        var composer = Substitute.For<ITransactionalEmailComposer>();
        composer.ReturnRenderedEmail();

        var deliveryHandler = Substitute.For<ICommandHandler<PrepareEmailDeliveryCommand>>();
        var sut = new AttendeeRegisteredIntegrationEventHandler(composer, deliveryHandler);

        var evt = Event();
        await sut.HandleAsync(evt, testContext.CancellationToken);

        var captured = (TicketConfirmationIntent)composer.ReceivedCalls().Single().GetArguments()[0]!;
        captured.FirstName.ShouldBe("Alice");
        var delivery = deliveryHandler.ReceivedDelivery();
        delivery.RecipientName.ShouldBe("Alice Anderson");
    }

    // Given an attendee has completed registration
    // When the registration confirmation event is processed
    // Then the ticket confirmation keeps the attendee's registration details
    [TestMethod]
    public async Task AttendeeRegistered_IntentIncludesTicketFacts()
    {
        var composer = Substitute.For<ITransactionalEmailComposer>();
        composer.ReturnRenderedEmail();

        var deliveryHandler = Substitute.For<ICommandHandler<PrepareEmailDeliveryCommand>>();
        var sut = new AttendeeRegisteredIntegrationEventHandler(composer, deliveryHandler);

        await sut.HandleAsync(Event(), testContext.CancellationToken);

        var captured = (TicketConfirmationIntent)composer.ReceivedCalls().Single().GetArguments()[0]!;
        captured.TicketTypes.ShouldBeEmpty();
        var delivery = deliveryHandler.ReceivedDelivery();
        delivery.RegistrationId.ShouldBe(RegId);
    }
}
