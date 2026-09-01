using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTransactionalEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

internal sealed class CouponEmailAdapterFixture
{
    private CouponEmailAdapterFixture(
        Guid teamId,
        Guid eventId,
        ITransactionalEmailComposer composer,
        ICommandHandler<PrepareEmailDeliveryCommand> deliveryHandler,
        CouponCreatedIntegrationEvent? couponCreatedEvent = null,
        CouponCreatedIntegrationEventHandler? couponCreatedHandler = null,
        WaitlistCouponIssuedIntegrationEvent? waitlistCouponIssuedEvent = null,
        WaitlistCouponIssuedIntegrationEventHandler? waitlistCouponIssuedHandler = null)
    {
        TeamId = teamId;
        EventId = eventId;
        Composer = composer;
        DeliveryHandler = deliveryHandler;
        CouponCreatedEvent = couponCreatedEvent;
        CouponCreatedHandler = couponCreatedHandler;
        WaitlistCouponIssuedEvent = waitlistCouponIssuedEvent;
        WaitlistCouponIssuedHandler = waitlistCouponIssuedHandler;
    }

    public Guid TeamId { get; }

    public Guid EventId { get; }

    public ITransactionalEmailComposer Composer { get; }
    public ICommandHandler<PrepareEmailDeliveryCommand> DeliveryHandler { get; }

    public CouponCreatedIntegrationEvent? CouponCreatedEvent { get; }

    public CouponCreatedIntegrationEventHandler? CouponCreatedHandler { get; }

    public WaitlistCouponIssuedIntegrationEvent? WaitlistCouponIssuedEvent { get; }

    public WaitlistCouponIssuedIntegrationEventHandler? WaitlistCouponIssuedHandler { get; }

    public static CouponEmailAdapterFixture OrganizerCreatedCoupon()
    {
        var teamId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var eventId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var composer = Substitute.For<ITransactionalEmailComposer>();
        composer.ReturnRenderedEmail(BuiltInEmailTemplateNames.CouponInvitation);
        var deliveryHandler = Substitute.For<ICommandHandler<PrepareEmailDeliveryCommand>>();
        var integrationEvent = new CouponCreatedIntegrationEvent(
            teamId,
            eventId,
            "alice@example.com",
            "GENERAL-123");

        return new CouponEmailAdapterFixture(
            teamId,
            eventId,
            composer,
            deliveryHandler,
            couponCreatedEvent: integrationEvent,
            couponCreatedHandler: new CouponCreatedIntegrationEventHandler(composer, deliveryHandler));
    }

    public static CouponEmailAdapterFixture WaitlistCouponIssued()
    {
        var teamId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var eventId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var composer = Substitute.For<ITransactionalEmailComposer>();
        composer.ReturnRenderedEmail(BuiltInEmailTemplateNames.WaitlistNotification);
        var deliveryHandler = Substitute.For<ICommandHandler<PrepareEmailDeliveryCommand>>();
        var expiresAt = new DateTimeOffset(2026, 9, 5, 14, 30, 0, TimeSpan.Zero);
        var integrationEvent = new WaitlistCouponIssuedIntegrationEvent(
            teamId,
            eventId,
            "bob@example.com",
            "WAIT-456",
            "Conference Pass",
            expiresAt);

        return new CouponEmailAdapterFixture(
            teamId,
            eventId,
            composer,
            deliveryHandler,
            waitlistCouponIssuedEvent: integrationEvent,
            waitlistCouponIssuedHandler: new WaitlistCouponIssuedIntegrationEventHandler(composer, deliveryHandler));
    }
}
