using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Composing;

[TestClass]
public sealed class TransactionalEmailComposerTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an event is ready for email and has no team branding
    // When a ticket confirmation email is created
    // Then the attendee receives the default-branded ticket details
    [TestMethod]
    public async ValueTask ComposeAsync_TicketConfirmation_ReturnsRenderedContentOnly()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = TransactionalEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        var rendered = await fixture.BuildComposer(Environment)
            .ComposeAsync(new TicketConfirmationIntent(
                teamId, eventId, RegistrationId.New(), "Alice", ["General Admission"], []),
                testContext.CancellationToken);

        rendered.EmailType.ShouldBe(BuiltInEmailTemplateNames.TicketConfirmation);
        rendered.Subject.ShouldBe("Admitto: Your DevConf Ticket");
        rendered.TextBody.ShouldContain("Your registration has been confirmed. Here's your ticket for DevConf!");
        rendered.TextBody.ShouldContain("- General Admission");
        rendered.HtmlBody.ShouldContain("Your DevConf");
        rendered.HtmlBody.ShouldContain("Alice");
        rendered.HtmlBody.ShouldContain("#2563eb");
        (await Environment.EmailDatabase.Context.EmailLog.CountAsync(testContext.CancellationToken)).ShouldBe(0);
        (await Environment.EmailDatabase.Context.OutboxMessages.CountAsync(testContext.CancellationToken)).ShouldBe(0);
    }

    // Given an attendee holds a confirmed ticket type and is on another ticket type's waitlist
    // When a ticket confirmation email is created
    // Then the confirmed and waitlisted ticket types are listed in separate sections
    [TestMethod]
    public async ValueTask ComposeAsync_TicketConfirmationWithWaitlistedTicketTypes_RendersSeparateSections()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = TransactionalEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        var rendered = await fixture.BuildComposer(Environment)
            .ComposeAsync(new TicketConfirmationIntent(
                teamId, eventId, RegistrationId.New(), "Alice", ["General Admission"], ["Workshop"]),
                testContext.CancellationToken);

        rendered.EmailType.ShouldBe(BuiltInEmailTemplateNames.TicketConfirmation);
        var text = rendered.TextBody;
        text.IndexOf("Your confirmed ticket type(s):", StringComparison.Ordinal).ShouldBeLessThan(
            text.IndexOf("- General Admission", StringComparison.Ordinal));
        text.IndexOf("- General Admission", StringComparison.Ordinal).ShouldBeLessThan(
            text.IndexOf("You're on the waitlist for:", StringComparison.Ordinal));
        text.IndexOf("You're on the waitlist for:", StringComparison.Ordinal).ShouldBeLessThan(
            text.IndexOf("- Workshop", StringComparison.Ordinal));
        rendered.TextBody.ShouldContain("not a confirmed ticket");
        rendered.HtmlBody.ShouldContain("Your confirmed ticket type(s)");
        rendered.HtmlBody.ShouldContain("You're on the waitlist for");
        rendered.HtmlBody.ShouldContain("<li>Workshop</li>");
    }

    // Given an attendee holds no confirmed ticket type and is only on a waitlist
    // When a ticket confirmation email is created
    // Then a waitlist confirmation is rendered with no confirmed-ticket language or QR code
    [TestMethod]
    public async ValueTask ComposeAsync_TicketConfirmationWithOnlyWaitlistedTicketTypes_RendersWaitlistConfirmation()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var registrationId = RegistrationId.New();
        var fixture = TransactionalEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        var rendered = await fixture.BuildComposer(Environment)
            .ComposeAsync(new TicketConfirmationIntent(
                teamId, eventId, registrationId, "Alice", [], ["Workshop"]),
                testContext.CancellationToken);

        rendered.EmailType.ShouldBe(BuiltInEmailTemplateNames.WaitlistConfirmation);
        rendered.Subject.ShouldBe("Admitto: You're on the DevConf waitlist");
        rendered.TextBody.ShouldContain("You're on the waitlist for:");
        rendered.TextBody.ShouldContain("- Workshop");
        rendered.TextBody.ShouldContain("don't have a confirmed ticket for DevConf yet");
        rendered.TextBody.ShouldNotContain("Your registration has been confirmed");
        rendered.TextBody.ShouldNotContain("QR code");
        rendered.HtmlBody.ShouldContain("<li>Workshop</li>");
        rendered.HtmlBody.ShouldNotContain("Your registration has been confirmed");
        rendered.HtmlBody.ShouldNotContain("QR");
        rendered.HtmlBody.ShouldNotContain("qr-code");
    }

    // Given an event is ready to accept registrations
    // When a coupon invitation email is created
    // Then the invitation includes a registration link with the coupon code as a query parameter
    // and does not render the coupon code as readable text
    [TestMethod]
    public async ValueTask ComposeAsync_CouponInvitation_RendersInvitation()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = TransactionalEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        var rendered = await fixture.BuildComposer(Environment).ComposeAsync(
            new CouponInvitationIntent(teamId, eventId, "INVITE-123"),
            testContext.CancellationToken);

        rendered.EmailType.ShouldBe(BuiltInEmailTemplateNames.CouponInvitation);
        rendered.Subject.ShouldBe("You're invited to DevConf");
        rendered.TextBody.ShouldContain("https://public.example/e/devconf/register?coupon=INVITE-123");
        rendered.TextBody.ShouldNotContain("coupon code: INVITE-123");
        rendered.HtmlBody.ShouldContain("You're invited to DevConf");
        rendered.HtmlBody.ShouldContain("href=\"https://public.example/e/devconf/register?coupon=INVITE-123\"");
    }

    // Given a place has opened for an attendee on the waitlist
    // When a waitlist offer email is created
    // Then the offer includes a claim link with the coupon code as a query parameter, the ticket type, and expiry
    // and does not render the coupon code as readable text
    [TestMethod]
    public async ValueTask ComposeAsync_WaitlistOffer_RendersOfferFacts()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var registrationId = RegistrationId.New();
        var fixture = TransactionalEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);
        var expiresAt = new DateTimeOffset(2026, 9, 5, 14, 30, 0, TimeSpan.Zero);

        var rendered = await fixture.BuildComposer(Environment).ComposeAsync(
            new WaitlistOfferIntent(
                teamId, eventId, "WAIT-456", "Conference Pass", expiresAt,
                WaitlistOfferReason.AutomaticPromotion, RegistrationId: registrationId),
            testContext.CancellationToken);

        rendered.EmailType.ShouldBe(BuiltInEmailTemplateNames.WaitlistNotification);
        rendered.Subject.ShouldBe("Your spot at DevConf is ready — use your coupon");
        rendered.TextBody.ShouldContain($"https://public.example/e/devconf/edit/{registrationId.Value}?coupon=WAIT-456");
        rendered.TextBody.ShouldNotContain("coupon code: WAIT-456");
        rendered.TextBody.ShouldContain("- Conference Pass");
        rendered.TextBody.ShouldContain("5 September 2026, 14:30 (UTC)");
        rendered.HtmlBody.ShouldContain("Your spot at DevConf is ready!");
        rendered.HtmlBody.ShouldContain($"href=\"https://public.example/e/devconf/edit/{registrationId.Value}?coupon=WAIT-456\"");
        rendered.HtmlBody.ShouldContain("Conference Pass");
        rendered.HtmlBody.ShouldContain("5 September 2026, 14:30 (UTC)");
    }

    // Given a place has opened for an attendee via a VIP promotion
    // When a waitlist offer email is created
    // Then it does not claim the spot goes to "the next person", since a VIP offer skipped the queue
    [TestMethod]
    public async ValueTask ComposeAsync_WaitlistOfferVipPromotion_OmitsNextPersonWording()
    {
        await AssertOmitsNextPersonWordingAsync(WaitlistOfferReason.VipPromotion);
    }

    // Given a place has opened for an attendee via a capacity-opened-for-everyone release
    // When a waitlist offer email is created
    // Then it does not claim the spot goes to "the next person", since everyone already got their own offer
    [TestMethod]
    public async ValueTask ComposeAsync_WaitlistOfferCapacityOpenedForEveryone_OmitsNextPersonWording()
    {
        await AssertOmitsNextPersonWordingAsync(WaitlistOfferReason.CapacityOpenedForEveryone);
    }

    private async ValueTask AssertOmitsNextPersonWordingAsync(WaitlistOfferReason reason)
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = TransactionalEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);
        var expiresAt = new DateTimeOffset(2026, 9, 5, 14, 30, 0, TimeSpan.Zero);

        var rendered = await fixture.BuildComposer(Environment).ComposeAsync(
            new WaitlistOfferIntent(
                teamId, eventId, "WAIT-456", "Conference Pass", expiresAt, reason, RegistrationId: RegistrationId.New()),
            testContext.CancellationToken);

        rendered.TextBody.ShouldNotContain("next person");
        rendered.HtmlBody.ShouldNotContain("next person");
    }

    // Given the queue has moved past an attendee whose waitlist offer went unclaimed
    // When a waitlist-offer-expired email is created
    // Then it tells the attendee their offer for the ticket type has lapsed, without offering a coupon
    [TestMethod]
    public async ValueTask ComposeAsync_WaitlistOfferExpired_RendersExpiredOffer()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = TransactionalEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        var rendered = await fixture.BuildComposer(Environment).ComposeAsync(
            new WaitlistOfferExpiredIntent(teamId, eventId, "Conference Pass", RegistrationClosed: false),
            testContext.CancellationToken);

        rendered.EmailType.ShouldBe(BuiltInEmailTemplateNames.WaitlistOfferExpired);
        rendered.Subject.ShouldBe("Your waitlist offer for DevConf has expired");
        rendered.TextBody.ShouldContain("Ticket type: Conference Pass");
        rendered.TextBody.ShouldContain("was not claimed in time");
        rendered.TextBody.ShouldContain("register again");
        rendered.TextBody.ShouldNotContain("coupon code:");
        rendered.HtmlBody.ShouldContain("Your waitlist offer for DevConf has expired");
        rendered.HtmlBody.ShouldContain("Conference Pass");
        rendered.HtmlBody.ShouldNotContain("Your spot at DevConf is ready");
    }

    // Given an attendee whose waitlist offer lapsed after registration closed
    // When a waitlist-offer-expired email is created
    // Then it says registration has closed instead of inviting them to register again
    [TestMethod]
    public async ValueTask ComposeAsync_WaitlistOfferExpiredAfterClose_DoesNotInviteToRegisterAgain()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = TransactionalEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        var rendered = await fixture.BuildComposer(Environment).ComposeAsync(
            new WaitlistOfferExpiredIntent(teamId, eventId, "Conference Pass", RegistrationClosed: true),
            testContext.CancellationToken);

        rendered.TextBody.ShouldContain("Registration for this event has closed.");
        rendered.TextBody.ShouldNotContain("register again");
        rendered.HtmlBody.ShouldContain("Registration for this event has closed.");
        rendered.HtmlBody.ShouldNotContain("register again");
    }

    // Given an attendee who was only on a waitlist has cancelled their registration
    // When a waitlist-cancellation email is created
    // Then the message describes removal from the waitlist rather than a cancelled ticket
    [TestMethod]
    public async ValueTask ComposeAsync_WaitlistCancellation_RendersWaitlistRemoval()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var registrationId = RegistrationId.New();
        var fixture = TransactionalEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        var rendered = await fixture.BuildComposer(Environment).ComposeAsync(
            new WaitlistCancellationIntent(teamId, eventId, "Alice", registrationId),
            testContext.CancellationToken);

        rendered.EmailType.ShouldBe(BuiltInEmailTemplateNames.WaitlistCancellation);
        rendered.Subject.ShouldBe("You've been removed from the DevConf waitlist");
        rendered.TextBody.ShouldContain("Alice");
        rendered.TextBody.ShouldContain("removed you from the waitlist for DevConf");
        rendered.TextBody.ShouldContain("https://public.example/e/devconf/register");
        rendered.TextBody.ShouldNotContain("Your DevConf Registration Has Been Cancelled");
        rendered.TextBody.ShouldNotContain("sorry you can’t make it");
        rendered.HtmlBody.ShouldContain("You've been removed from the DevConf waitlist");
        rendered.HtmlBody.ShouldContain("href=\"https://public.example/e/devconf/register\"");
    }

    // Given an attendee has cancelled their registration
    // When an attendee-cancellation email is created
    // Then the message confirms the cancellation and offers a registration link
    [TestMethod]
    public async ValueTask ComposeAsync_AttendeeCancellation_RendersCancellation()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var registrationId = RegistrationId.New();
        var fixture = TransactionalEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        var rendered = await fixture.BuildComposer(Environment).ComposeAsync(
            new AttendeeRequestCancellationIntent(teamId, eventId, "Alice", registrationId),
            testContext.CancellationToken);

        rendered.EmailType.ShouldBe(BuiltInEmailTemplateNames.Cancellation);
        rendered.Subject.ShouldBe("Your DevConf Registration Has Been Cancelled");
        rendered.TextBody.ShouldContain("We’ve processed your request to cancel your registration for DevConf.");
        rendered.TextBody.ShouldContain("Alice");
        rendered.TextBody.ShouldContain("https://public.example/e/devconf/register");
        rendered.HtmlBody.ShouldContain("Your DevConf Registration Has Been Cancelled");
        rendered.HtmlBody.ShouldContain("Alice");
        rendered.HtmlBody.ShouldContain("href=\"https://public.example/e/devconf/register\"");
    }

    // Given an attendee has not reconfirmed their attendance in time
    // When a reconfirm auto-cancellation email is created
    // Then the message explains why the registration was cancelled
    [TestMethod]
    public async ValueTask ComposeAsync_ReconfirmAutoCancellation_RendersReason()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = TransactionalEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        var rendered = await fixture.BuildComposer(Environment).ComposeAsync(
            new ReconfirmAutoCancellationIntent(teamId, eventId, "Alice", RegistrationId.New()),
            testContext.CancellationToken);

        rendered.EmailType.ShouldBe(BuiltInEmailTemplateNames.ReconfirmCancelled);
        rendered.Subject.ShouldBe("Your DevConf Registration Has Been Cancelled");
        rendered.TextBody.ShouldContain("automatically cancelled because we did not receive a reconfirmation");
        rendered.TextBody.ShouldContain("https://public.example/e/devconf/register");
        rendered.HtmlBody.ShouldContain("automatically cancelled because we did not receive a reconfirmation");
        rendered.HtmlBody.ShouldContain("Still interested in attending?");
    }

    // Given an attendee's visa letter request has been denied
    // When a visa-letter-denied cancellation email is created
    // Then the message explains the cancellation
    [TestMethod]
    public async ValueTask ComposeAsync_VisaLetterDeniedCancellation_RendersReason()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = TransactionalEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        var rendered = await fixture.BuildComposer(Environment).ComposeAsync(
            new VisaLetterDeniedCancellationIntent(teamId, eventId, "Alice", RegistrationId.New()),
            testContext.CancellationToken);

        rendered.EmailType.ShouldBe(BuiltInEmailTemplateNames.VisaLetterDenied);
        rendered.Subject.ShouldBe("Your DevConf Registration Has Been Cancelled");
        rendered.TextBody.ShouldContain("unable to provide business invitation letters for visa purposes");
        rendered.TextBody.ShouldContain("Alice");
        rendered.HtmlBody.ShouldContain("unable to provide business invitation letters for visa purposes");
        rendered.HtmlBody.ShouldContain("Your current registration will be canceled automatically.");
    }

    // Given an attendee has requested a verification code for a branded event
    // When a verification-code email is created
    // Then the email includes the code and event branding
    [TestMethod]
    public async ValueTask ComposeAsync_VerificationCode_UsesProjectedBranding()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = TransactionalEmailComposerFixture.ExistingTeamContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        var rendered = await fixture.BuildComposer(Environment).ComposeAsync(
            new VerificationCodeIntent(teamId, eventId, "123456"), testContext.CancellationToken);

        rendered.EmailType.ShouldBe(BuiltInEmailTemplateNames.VerificationCode);
        rendered.Subject.ShouldBe("Your DevConf registration code");
        rendered.TextBody.ShouldContain("Your code is:");
        rendered.TextBody.ShouldContain("123456");
        rendered.TextBody.ShouldContain("The DevConf Team");
        rendered.HtmlBody.ShouldContain("123456");
        rendered.HtmlBody.ShouldContain("Your registration code");
        rendered.HtmlBody.ShouldContain("#0f766e");
    }

    // Given an event is ready to accept registrations and no public-link base URL is configured
    // When a coupon invitation email is created
    // Then its registration link uses localhost
    [TestMethod]
    public async ValueTask ComposeAsync_DefaultPublicLinkBaseUrl_UsesLocalhost()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = TransactionalEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        var rendered = await fixture.BuildComposerWithDefaultPublicLink(Environment).ComposeAsync(
            new CouponInvitationIntent(teamId, eventId, "LOCAL-123"),
            testContext.CancellationToken);

        rendered.TextBody.ShouldContain("http://localhost/devconf/register?coupon=LOCAL-123");
    }

    // Given an event projection is missing required details
    // When a cancellation email is created
    // Then no email delivery claim is created
    [TestMethod]
    public async ValueTask ComposeAsync_IncompleteEventContext_LeavesDeliveryBoundaryUntouched()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = TransactionalEmailComposerFixture.IncompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        await Should.ThrowAsync<EventEmailContextMissingException>(async () =>
            await fixture.BuildComposer(Environment).ComposeAsync(
                new AttendeeRequestCancellationIntent(teamId, eventId, "Alice", RegistrationId.New()),
                testContext.CancellationToken));

        (await Environment.EmailDatabase.Context.EmailLog.CountAsync(testContext.CancellationToken)).ShouldBe(0);
        (await Environment.EmailDatabase.Context.OutboxMessages.CountAsync(testContext.CancellationToken)).ShouldBe(0);
    }

}
