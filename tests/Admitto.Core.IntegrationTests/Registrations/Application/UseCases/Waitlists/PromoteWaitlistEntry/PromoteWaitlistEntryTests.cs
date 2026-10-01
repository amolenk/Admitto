using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;
using Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Application.Messaging;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.UpdatePartnerRegistration;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.PromoteWaitlistEntry;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.PromoteWaitlistEntry;

[TestClass]
public sealed class PromoteWaitlistEntryTests(TestContext testContext) : AspireIntegrationTestBase
{
    private PromoteWaitlistEntryHandler CreateSut() =>
        new(Environment.RegistrationsDatabase.Context, TimeProvider.System);

    private static PromoteWaitlistEntryCommand PromoteCommand(PromoteWaitlistEntryFixture fixture, Guid entryId) =>
        new(fixture.EventId.Value, fixture.TeamId.Value, fixture.WorkshopId.Value, entryId);

    // Given a sold-out workshop with three attendees queued and the VIP second in line
    // When an organizer promotes the VIP's waitlist entry
    // Then a waitlist coupon is issued to the VIP, their entry leaves the queue, and the others are renumbered
    [TestMethod]
    public async ValueTask PromoteWaitlistEntry_MidQueueEntry_IssuesWaitlistCouponAndRenumbersQueue()
    {
        // Arrange
        var fixture = PromoteWaitlistEntryFixture.WithVipSecondInQueue();
        await fixture.SetupAsync(Environment);

        // Act
        var couponId = await CreateSut().HandleAsync(
            PromoteCommand(fixture, fixture.VipEntryId.Value), testContext.CancellationToken);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var coupon = await dbContext.Coupons.SingleAsync(testContext.CancellationToken);
            coupon.Id.Value.ShouldBe(couponId);
            coupon.Email.ShouldBe(PromoteWaitlistEntryFixture.VipEmail);
            coupon.Source.ShouldBe(CouponSource.Waitlist);
            coupon.BypassRegistrationWindow.ShouldBeTrue();
            coupon.AllowedTicketTypeIds.ShouldBe([fixture.WorkshopId]);

            var waitlist = await dbContext.Waitlists
                .FirstAsync(w => w.Id == fixture.WorkshopId, testContext.CancellationToken);
            waitlist.HasActiveEntry(PromoteWaitlistEntryFixture.VipEmail).ShouldBeFalse();
            waitlist.GetActivePosition(PromoteWaitlistEntryFixture.FirstEmail).ShouldBe(1);
            waitlist.GetActivePosition(PromoteWaitlistEntryFixture.ThirdEmail).ShouldBe(2);
            waitlist.Coupons.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
                c => c.Id.ShouldBe(coupon.Id),
                c => c.Status.ShouldBe(WaitlistCouponStatus.Issued));
        });
    }

    // Given the VIP second in line on a sold-out workshop's waitlist
    // When an organizer promotes the VIP's waitlist entry
    // Then the VIP's waitlist offer email is prepared with the issued coupon and its expiry
    [TestMethod]
    public async ValueTask PromoteWaitlistEntry_MidQueueEntry_PreparesWaitlistOfferEmailForVip()
    {
        // Arrange
        var fixture = PromoteWaitlistEntryFixture.WithVipSecondInQueue();
        await fixture.SetupAsync(Environment);

        // Act
        await CreateSut().HandleAsync(
            PromoteCommand(fixture, fixture.VipEntryId.Value), testContext.CancellationToken);

        // Assert — the promotion raised a WaitlistCouponIssuedDomainEvent with the coupon details
        var waitlist = await Environment.RegistrationsDatabase.Context.Waitlists
            .FirstAsync(w => w.Id == fixture.WorkshopId, testContext.CancellationToken);
        var domainEvent = waitlist.GetDomainEvents()
            .OfType<WaitlistCouponIssuedDomainEvent>()
            .ShouldHaveSingleItem();

        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);
        var coupon = await Environment.RegistrationsDatabase.Context.Coupons
            .SingleAsync(testContext.CancellationToken);

        domainEvent.RecipientEmail.ShouldBe(PromoteWaitlistEntryFixture.VipEmail);
        domainEvent.CouponCode.ShouldBe(coupon.Code);
        domainEvent.ExpiresAt.ShouldBe(coupon.ExpiresAt);

        // Act — publish the domain event and hand the integration event to the email adapter
        var outbox = Substitute.For<IOutbox>();
        IIntegrationEvent? capturedIntegrationEvent = null;
        outbox.When(o => o.Enqueue(Arg.Any<IIntegrationEvent>()))
            .Do(ci => capturedIntegrationEvent = ci.Arg<IIntegrationEvent>());
        await new RegistrationsIntegrationEventPublisher(outbox)
            .HandleAsync(domainEvent, testContext.CancellationToken);

        var integrationEvent = capturedIntegrationEvent.ShouldBeOfType<WaitlistCouponIssuedIntegrationEvent>();

        var composer = Substitute.For<ITransactionalEmailComposer>();
        composer.ReturnRenderedEmail(BuiltInEmailTemplateNames.WaitlistNotification);
        var deliveryHandler = Substitute.For<ICommandHandler<PrepareEmailDeliveryCommand>>();
        await new WaitlistCouponIssuedIntegrationEventHandler(composer, deliveryHandler)
            .HandleAsync(integrationEvent, testContext.CancellationToken);

        // Assert — the regular waitlist offer email goes to the VIP
        var delivery = deliveryHandler.ReceivedDelivery();
        delivery.RecipientAddress.ShouldBe(PromoteWaitlistEntryFixture.VipEmail.Value);
        delivery.EmailType.ShouldBe(BuiltInEmailTemplateNames.WaitlistNotification);
        integrationEvent.TicketTypeName.ShouldBe("Workshop");
        integrationEvent.CouponCode.ShouldBe(coupon.Code.Value.ToString());
    }

    // Given the VIP was promoted and received a waitlist coupon for the sold-out workshop
    // When the VIP updates their registration to add the workshop using that coupon
    // Then the workshop ticket is granted as an admin ticket and the coupon is redeemed on the waitlist that issued it
    [TestMethod]
    public async ValueTask PromoteWaitlistEntry_CouponRedeemedViaUpdateRegistration_GrantsWorkshopTicket()
    {
        // Arrange
        var fixture = PromoteWaitlistEntryFixture.WithVipSecondInQueue();
        await fixture.SetupAsync(Environment);
        var dbContext = Environment.RegistrationsDatabase.Context;

        await CreateSut().HandleAsync(
            PromoteCommand(fixture, fixture.VipEntryId.Value), testContext.CancellationToken);
        await dbContext.SaveChangesAsync(testContext.CancellationToken);
        var couponCode = (await dbContext.Coupons.SingleAsync(testContext.CancellationToken)).Code.Value;

        // Act
        await new UpdatePartnerRegistrationHandler(dbContext, TimeProvider.System).HandleAsync(
            new UpdatePartnerRegistrationCommand(
                fixture.EventId.Value,
                fixture.TeamId.Value,
                fixture.VipRegistrationId.Value,
                "Vera",
                "Important",
                [fixture.EarlyBirdId.Value, fixture.WorkshopId.Value],
                [],
                new Dictionary<string, string>(),
                couponCode),
            testContext.CancellationToken);

        // Assert
        await Environment.RegistrationsDatabase.AssertAsync(async db =>
        {
            var registration = await db.Registrations
                .FirstAsync(r => r.Id == fixture.VipRegistrationId, testContext.CancellationToken);
            registration.Tickets.ShouldContain(t => t.Id == fixture.WorkshopId && t.Mode == ClaimMode.Admin);

            // A redeemed VIP offer is an admin ticket on top of public capacity.
            var workshop = (await db.TicketCatalogs.SingleAsync(testContext.CancellationToken))
                .GetTicketType(fixture.WorkshopId)!;
            workshop.AdminUsedCount.ShouldBe(1);
            workshop.WaitlistHeldCapacity.ShouldBe(0);

            var coupon = await db.Coupons.SingleAsync(testContext.CancellationToken);
            coupon.RedeemedAt.ShouldNotBeNull();

            var waitlist = await db.Waitlists
                .FirstAsync(w => w.Id == fixture.WorkshopId, testContext.CancellationToken);
            waitlist.Coupons.ShouldHaveSingleItem().Status.ShouldBe(WaitlistCouponStatus.Redeemed);
            waitlist.GetActivePosition(PromoteWaitlistEntryFixture.FirstEmail).ShouldBe(1);
            waitlist.GetActivePosition(PromoteWaitlistEntryFixture.ThirdEmail).ShouldBe(2);
        });
    }

    // Given the VIP's waitlist entry was already promoted
    // When an organizer promotes the same entry again
    // Then it is rejected as no longer active and no second coupon is issued
    [TestMethod]
    public async ValueTask PromoteWaitlistEntry_EntryAlreadyPromoted_ThrowsEntryNotActive()
    {
        // Arrange
        var fixture = PromoteWaitlistEntryFixture.WithVipSecondInQueue();
        await fixture.SetupAsync(Environment);
        await CreateSut().HandleAsync(
            PromoteCommand(fixture, fixture.VipEntryId.Value), testContext.CancellationToken);
        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        // Act
        var result = await ErrorResult.CaptureAsync(async () => await CreateSut().HandleAsync(
            PromoteCommand(fixture, fixture.VipEntryId.Value), testContext.CancellationToken));

        // Assert
        result.Error.ShouldMatch(Waitlist.Errors.EntryNotActive);
        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
            (await dbContext.Coupons.CountAsync(testContext.CancellationToken)).ShouldBe(1));
    }

    // Given a waitlist without the requested entry
    // When an organizer promotes that entry
    // Then it is rejected as no longer active
    [TestMethod]
    public async ValueTask PromoteWaitlistEntry_UnknownEntry_ThrowsEntryNotActive()
    {
        // Arrange
        var fixture = PromoteWaitlistEntryFixture.WithVipSecondInQueue();
        await fixture.SetupAsync(Environment);

        // Act
        var result = await ErrorResult.CaptureAsync(async () => await CreateSut().HandleAsync(
            PromoteCommand(fixture, Guid.NewGuid()), testContext.CancellationToken));

        // Assert
        result.Error.ShouldMatch(Waitlist.Errors.EntryNotActive);
    }
}
