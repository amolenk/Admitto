using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Application.Templating;
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
                teamId, eventId, RegistrationId.New(), "Alice", ["General Admission"]),
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

    // Given an event is ready to accept registrations
    // When a coupon invitation email is created
    // Then the invitation includes its code and registration link
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
        rendered.TextBody.ShouldContain("Your coupon code: INVITE-123");
        rendered.TextBody.ShouldContain("https://public.example/e/devconf/register");
        rendered.HtmlBody.ShouldContain("You're invited to DevConf");
        rendered.HtmlBody.ShouldContain("INVITE-123");
        rendered.HtmlBody.ShouldContain("href=\"https://public.example/e/devconf/register\"");
    }

    // Given a place has opened for an attendee on the waitlist
    // When a waitlist offer email is created
    // Then the offer includes its coupon, ticket type, and expiry
    [TestMethod]
    public async ValueTask ComposeAsync_WaitlistOffer_RendersOfferFacts()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = TransactionalEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);
        var expiresAt = new DateTimeOffset(2026, 9, 5, 14, 30, 0, TimeSpan.Zero);

        var rendered = await fixture.BuildComposer(Environment).ComposeAsync(
            new WaitlistOfferIntent(teamId, eventId, "WAIT-456", "Conference Pass", expiresAt),
            testContext.CancellationToken);

        rendered.EmailType.ShouldBe(BuiltInEmailTemplateNames.WaitlistNotification);
        rendered.Subject.ShouldBe("Your spot at DevConf is ready — use your coupon");
        rendered.TextBody.ShouldContain("Your personal coupon code: WAIT-456");
        rendered.TextBody.ShouldContain("Ticket type: Conference Pass");
        rendered.TextBody.ShouldContain("September 5, 2026");
        rendered.HtmlBody.ShouldContain("Your spot at DevConf is ready!");
        rendered.HtmlBody.ShouldContain("WAIT-456");
        rendered.HtmlBody.ShouldContain("Conference Pass");
        rendered.HtmlBody.ShouldContain("September 5, 2026");
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

        rendered.TextBody.ShouldContain("http://localhost/devconf/register");
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
