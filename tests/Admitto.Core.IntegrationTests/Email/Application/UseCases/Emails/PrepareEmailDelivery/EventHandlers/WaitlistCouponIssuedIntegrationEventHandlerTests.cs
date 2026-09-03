using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;

[TestClass]
public sealed class WaitlistCouponIssuedIntegrationEventHandlerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    // Given a place has opened for an attendee on the waitlist
    // When the waitlist offer event is processed
    // Then the attendee receives a waitlist offer with its coupon and expiry
    [TestMethod]
    public async Task HandleAsync_WaitlistCoupon_UsesWaitlistOfferIntent()
    {
        var fixture = CouponEmailAdapterFixture.WaitlistCouponIssued();
        var integrationEvent = fixture.WaitlistCouponIssuedEvent!;

        await fixture.WaitlistCouponIssuedHandler!.HandleAsync(
            integrationEvent,
            testContext.CancellationToken);

        await fixture.Composer.Received(1).ComposeAsync(
            Arg.Is<WaitlistOfferIntent>(intent =>
                intent!.TeamId == TeamId.From(fixture.TeamId)
                && intent.TicketedEventId == TicketedEventId.From(fixture.EventId)
                && intent.CouponCode == "WAIT-456"
                && intent.TicketTypeName == "Conference Pass"
                && intent.ExpiresAt == integrationEvent.ExpiresAt),
            Arg.Any<CancellationToken>());

        var delivery = fixture.DeliveryHandler.ReceivedDelivery();
        delivery.RecipientAddress.ShouldBe("bob@example.com");
        delivery.IdempotencyKey.ShouldBe(
            $"waitlist-coupon-issued:{fixture.TeamId}:{fixture.EventId}:WAIT-456");
        delivery.EmailType.ShouldBe(BuiltInEmailTemplateNames.WaitlistNotification);
        delivery.RegistrationId.ShouldBeNull();
    }
}
