using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeRegistrationCancellation;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

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

    // Given an attendee asks to cancel a registration
    // When the cancellation message is processed
    // Then the attendee cancellation email uses their name and address
    [TestMethod]
    public async Task HandleAsync_AttendeeRequest_UsesTypedIntentAndDelivery()
    {
        var composer = Substitute.For<IRegistrationCancellationEmailComposer>();
        var sut = new RegistrationCancelledIntegrationEventHandler(composer);

        await sut.HandleAsync(Event("AttendeeRequest"), testContext.CancellationToken);

        await composer.Received(1).ComposeAsync(
            TeamGuid,
            EventGuid,
            Arg.Is<AttendeeRequestCancellationIntent>(intent => intent != null && intent.FirstName == "Alice"),
            Arg.Is<RegistrationCancellationDelivery>(delivery =>
                delivery != null && delivery.RegistrationId.Value == RegistrationGuid
                && delivery.RecipientAddress == "alice@example.com"
                && delivery.RecipientName == "Alice Test"
                && delivery.IdempotencyKey == "registration-cancelled:11111111111111111111111111111111"),
            Arg.Any<CancellationToken>());
    }

    // Given an attendee is denied a visa invitation letter
    // When the cancellation message is processed
    // Then the attendee's visa-denial email uses their first name
    [TestMethod]
    public async Task HandleAsync_VisaLetterDenied_UsesTypedIntent()
    {
        var composer = Substitute.For<IRegistrationCancellationEmailComposer>();
        var sut = new RegistrationCancelledIntegrationEventHandler(composer);

        await sut.HandleAsync(Event("VisaLetterDenied"), testContext.CancellationToken);

        await composer.Received(1).ComposeAsync(
            TeamGuid,
            EventGuid,
            Arg.Is<VisaLetterDeniedCancellationIntent>(intent => intent != null && intent.FirstName == "Alice"),
            Arg.Any<RegistrationCancellationDelivery>(),
            Arg.Any<CancellationToken>());
    }

    // Given an attendee does not reconfirm in time
    // When the automatic cancellation message is processed
    // Then the attendee's cancellation email uses the reconfirm reason
    [TestMethod]
    public async Task HandleAsync_ReconfirmAutoCancel_UsesTypedIntent()
    {
        var composer = Substitute.For<IRegistrationCancellationEmailComposer>();
        var sut = new RegistrationCancelledIntegrationEventHandler(composer);

        await sut.HandleAsync(Event("ReconfirmAutoCancel"), testContext.CancellationToken);

        await composer.Received(1).ComposeAsync(
            TeamGuid,
            EventGuid,
            Arg.Is<ReconfirmAutoCancellationIntent>(intent => intent != null && intent.FirstName == "Alice"),
            Arg.Any<RegistrationCancellationDelivery>(),
            Arg.Any<CancellationToken>());
    }

    // Given the same cancellation arrives in two integration messages with different message IDs
    // When both messages are processed
    // Then each message ID produces its own idempotency key
    [TestMethod]
    public async Task HandleAsync_DifferentIntegrationMessageIds_UsesEachForIdempotency()
    {
        var composer = Substitute.For<IRegistrationCancellationEmailComposer>();
        var second = Event("ReconfirmAutoCancel") with
        {
            IntegrationEventId = Guid.Parse("22222222-2222-2222-2222-222222222222")
        };
        var sut = new RegistrationCancelledIntegrationEventHandler(composer);

        await sut.HandleAsync(Event("ReconfirmAutoCancel"), testContext.CancellationToken);
        await sut.HandleAsync(second, testContext.CancellationToken);

        var deliveries = composer.ReceivedCalls()
            .Select(call => call.GetArguments()[3])
            .OfType<RegistrationCancellationDelivery>()
            .ToList();
        deliveries.Count.ShouldBe(2);
        deliveries[0].IdempotencyKey.ShouldBe("registration-cancelled:11111111111111111111111111111111");
        deliveries[1].IdempotencyKey.ShouldBe("registration-cancelled:22222222222222222222222222222222");
    }

    // Given an attendee's registration is cancelled because tickets were removed
    // When the cancellation message is processed
    // Then no cancellation email work is created
    [TestMethod]
    public async Task HandleAsync_TicketTypesRemoved_SilentlySkipsCancellationEmail()
    {
        var composer = Substitute.For<IRegistrationCancellationEmailComposer>();
        var sut = new RegistrationCancelledIntegrationEventHandler(composer);

        await sut.HandleAsync(Event("TicketTypesRemoved"), testContext.CancellationToken);

        await composer.DidNotReceiveWithAnyArgs().ComposeAsync(
            TeamGuid,
            EventGuid,
            default!,
            default!,
            default);
    }
}
