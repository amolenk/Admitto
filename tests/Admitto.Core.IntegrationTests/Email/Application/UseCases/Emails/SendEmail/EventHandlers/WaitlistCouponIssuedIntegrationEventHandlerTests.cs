using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeCouponEmail;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

[TestClass]
public sealed class WaitlistCouponIssuedIntegrationEventHandlerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    // Given a waitlist coupon issued integration event
    // When the event is handled
    // Then a distinct typed waitlist offer intent preserves all offer facts and the existing idempotency key
    [TestMethod]
    public async Task HandleAsync_WaitlistCoupon_UsesWaitlistOfferIntentAndDelivery()
    {
        var fixture = CouponEmailAdapterFixture.WaitlistCouponIssued();
        var integrationEvent = fixture.WaitlistCouponIssuedEvent!;

        await fixture.WaitlistCouponIssuedHandler!.HandleAsync(
            integrationEvent,
            testContext.CancellationToken);

        await fixture.Composer.Received(1).ComposeAsync(
            TeamId.From(fixture.TeamId),
            TicketedEventId.From(fixture.EventId),
            Arg.Is<WaitlistOfferIntent>(intent =>
                intent!.CouponCode == "WAIT-456"
                && intent.TicketTypeName == "Conference Pass"
                && intent.ExpiresAt == integrationEvent.ExpiresAt),
            Arg.Is<CouponEmailDelivery>(delivery =>
                delivery!.RecipientAddress == "bob@example.com"
                && delivery.RecipientName == "bob@example.com"
                && delivery.IdempotencyKey == $"waitlist-coupon-issued:{fixture.TeamId}:{fixture.EventId}:WAIT-456"),
            Arg.Any<CancellationToken>());
    }
}
