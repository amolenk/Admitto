using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTicketConfirmation;
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

    // Given an AttendeeRegistered integration event for an attendee
    // When the event is handled
    // Then a ticket confirmation email is sent to the attendee with an idempotency key derived from the registration
    [TestMethod]
    public async Task AttendeeRegistered_DispatchesTicketEmail()
    {
        var composer = Substitute.For<ITicketConfirmationEmailComposer>();

        var sut = new AttendeeRegisteredIntegrationEventHandler(composer);

        var evt = Event();
        await sut.HandleAsync(evt, testContext.CancellationToken);

        await composer.Received(1).ComposeAsync(
            TeamGuid,
            EventGuid,
            Arg.Is<TicketConfirmationIntent>(i => i!.RegistrationId == RegistrationId.From(RegId)),
            Arg.Is<TicketConfirmationDelivery>(d =>
                d!.RecipientAddress == "alice@example.com" &&
                d.RecipientName == "Alice Anderson" &&
                d.IdempotencyKey == $"attendee-registered:{RegId}:{evt.RegisteredAt:O}"),
            Arg.Any<CancellationToken>());
    }

    // Given an AttendeeRegistered integration event for an attendee
    // When the event is handled
    // Then the shared intent includes the attendee's first and last names
    [TestMethod]
    public async Task AttendeeRegistered_IntentIncludesAttendeeFacts()
    {
        var composer = Substitute.For<ITicketConfirmationEmailComposer>();

        var sut = new AttendeeRegisteredIntegrationEventHandler(composer);

        await sut.HandleAsync(Event(), testContext.CancellationToken);

        var captured = (TicketConfirmationIntent)composer.ReceivedCalls().Single().GetArguments()[2]!;
        captured.FirstName.ShouldBe("Alice");
        captured.FirstName.ShouldBe("Alice");
    }

    // Given an AttendeeRegistered integration event for an attendee
    // When the event is handled
    // Then the shared intent includes the attendee's ticket facts
    [TestMethod]
    public async Task AttendeeRegistered_IntentIncludesTicketFacts()
    {
        var composer = Substitute.For<ITicketConfirmationEmailComposer>();

        var sut = new AttendeeRegisteredIntegrationEventHandler(composer);

        await sut.HandleAsync(Event(), testContext.CancellationToken);

        var captured = (TicketConfirmationIntent)composer.ReceivedCalls().Single().GetArguments()[2]!;
        captured.TicketTypes.ShouldBeEmpty();
    }
}
