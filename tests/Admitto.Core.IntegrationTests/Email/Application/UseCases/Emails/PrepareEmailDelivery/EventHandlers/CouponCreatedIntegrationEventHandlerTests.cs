using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;

[TestClass]
public sealed class CouponCreatedIntegrationEventHandlerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    // Given an organizer has created a coupon for an attendee
    // When the coupon invitation event is processed
    // Then the attendee receives a coupon invitation email
    [TestMethod]
    public async Task HandleAsync_OrganizerCoupon_UsesCouponInvitationIntent()
    {
        var fixture = CouponEmailAdapterFixture.OrganizerCreatedCoupon();

        await fixture.CouponCreatedHandler!.HandleAsync(
            fixture.CouponCreatedEvent!,
            testContext.CancellationToken);

        await fixture.Composer.Received(1).ComposeAsync(
            Arg.Is<CouponInvitationIntent>(intent =>
                intent!.TeamId == TeamId.From(fixture.TeamId)
                && intent.TicketedEventId == TicketedEventId.From(fixture.EventId)
                && intent.CouponCode == "GENERAL-123"),
            Arg.Any<CancellationToken>());

        var delivery = fixture.DeliveryHandler.ReceivedDelivery();
        delivery.RecipientAddress.ShouldBe("alice@example.com");
        delivery.IdempotencyKey.ShouldBe("coupon-created:GENERAL-123");
        delivery.EmailType.ShouldBe(BuiltInEmailTemplateNames.CouponInvitation);
        delivery.RegistrationId.ShouldBeNull();
    }
}
