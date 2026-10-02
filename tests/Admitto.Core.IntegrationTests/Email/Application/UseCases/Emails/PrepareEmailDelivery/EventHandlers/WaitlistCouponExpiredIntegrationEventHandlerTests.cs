using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;

[TestClass]
public sealed class WaitlistCouponExpiredIntegrationEventHandlerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    // Given an attendee's waitlist offer lapsed unclaimed
    // When the waitlist-offer-expired event is processed
    // Then the attendee receives an expired-offer email keyed to that coupon
    [TestMethod]
    public async Task HandleAsync_ExpiredWaitlistCoupon_UsesWaitlistOfferExpiredIntent()
    {
        var teamId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var eventId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var composer = Substitute.For<ITransactionalEmailComposer>();
        composer.ReturnRenderedEmail(BuiltInEmailTemplateNames.WaitlistOfferExpired);
        var deliveryHandler = Substitute.For<ICommandHandler<PrepareEmailDeliveryCommand>>();
        var sut = new WaitlistCouponExpiredIntegrationEventHandler(composer, deliveryHandler);

        await sut.HandleAsync(
            new WaitlistCouponExpiredIntegrationEvent(teamId, eventId, "bob@example.com", "WAIT-456", "Conference Pass", RegistrationClosed: true),
            testContext.CancellationToken);

        await composer.Received(1).ComposeAsync(
            Arg.Is<WaitlistOfferExpiredIntent>(intent =>
                intent!.TeamId == TeamId.From(teamId)
                && intent.TicketedEventId == TicketedEventId.From(eventId)
                && intent.TicketTypeName == "Conference Pass"
                && intent.RegistrationClosed),
            Arg.Any<CancellationToken>());

        var delivery = deliveryHandler.ReceivedDelivery();
        delivery.RecipientAddress.ShouldBe("bob@example.com");
        delivery.IdempotencyKey.ShouldBe($"waitlist-coupon-expired:{teamId}:{eventId}:WAIT-456");
        delivery.EmailType.ShouldBe(BuiltInEmailTemplateNames.WaitlistOfferExpired);
        delivery.RegistrationId.ShouldBeNull();
    }
}
