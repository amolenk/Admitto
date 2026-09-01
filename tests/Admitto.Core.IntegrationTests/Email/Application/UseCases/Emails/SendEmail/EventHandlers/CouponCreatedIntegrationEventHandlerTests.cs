using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeCouponEmail;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

[TestClass]
public sealed class CouponCreatedIntegrationEventHandlerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    // Given an organizer-created coupon integration event
    // When the event is handled
    // Then a typed coupon invitation intent preserves the recipient and existing idempotency key
    [TestMethod]
    public async Task HandleAsync_OrganizerCoupon_UsesCouponInvitationIntentAndDelivery()
    {
        var fixture = CouponEmailAdapterFixture.OrganizerCreatedCoupon();

        await fixture.CouponCreatedHandler!.HandleAsync(
            fixture.CouponCreatedEvent!,
            testContext.CancellationToken);

        await fixture.Composer.Received(1).ComposeAsync(
            TeamId.From(fixture.TeamId),
            TicketedEventId.From(fixture.EventId),
            Arg.Is<CouponInvitationIntent>(intent => intent!.CouponCode == "GENERAL-123"),
            Arg.Is<CouponEmailDelivery>(delivery =>
                delivery!.RecipientAddress == "alice@example.com"
                && delivery.RecipientName == "alice@example.com"
                && delivery.IdempotencyKey == "coupon-created:GENERAL-123"),
            Arg.Any<CancellationToken>());
    }
}
