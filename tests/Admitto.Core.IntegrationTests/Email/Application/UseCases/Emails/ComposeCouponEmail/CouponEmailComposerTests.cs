using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeCouponEmail;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.ComposeCouponEmail;

[TestClass]
public sealed class CouponEmailComposerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    // Given a complete event and team context
    // When a coupon invitation is composed
    // Then the built-in invitation is rendered with event, coupon, and registration details without waitlist-only facts
    [TestMethod]
    public async ValueTask ComposeAsync_CouponInvitation_RendersInvitationThroughEventScope()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = CouponEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        await fixture.BuildComposer(Environment).ComposeAsync(
            teamId,
            eventId,
            new CouponInvitationIntent("GENERAL-123"),
            new CouponEmailDelivery(
                "alice@example.com",
                "alice@example.com",
                "coupon-created:GENERAL-123"),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        var log = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .SingleAsync(testContext.CancellationToken);
        log.EmailType.ShouldBe(BuiltInEmailTemplateNames.CouponInvitation);
        log.Subject.ShouldBe("You're invited to DevConf");

        var delivery = await Environment.EmailDatabase.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(testContext.CancellationToken);
        var text = delivery.Payload.RootElement.GetProperty("textBody").GetString()!;
        var html = delivery.Payload.RootElement.GetProperty("htmlBody").GetString()!;

        foreach (var body in new[] { text, html })
        {
            body.ShouldContain("DevConf");
            body.ShouldContain("GENERAL-123");
            body.ShouldContain("https://public.example/e/devconf/register");
            body.ShouldContain("https://devconf.example.com");
            var lowerBody = body.ToLowerInvariant();
            lowerBody.ShouldNotContain("waitlist");
            lowerBody.ShouldNotContain("position");
            lowerBody.ShouldNotContain("ticket type");
            lowerBody.ShouldNotContain("expires");
        }
        html.ShouldContain("#0f766e");
    }

    // Given a complete event and team context
    // When a waitlist offer is composed
    // Then the existing waitlist notification retains its coupon, ticket, expiry, and registration details
    [TestMethod]
    public async ValueTask ComposeAsync_WaitlistOffer_RendersExistingWaitlistNotificationThroughEventScope()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var expiresAt = new DateTimeOffset(2026, 9, 5, 14, 30, 0, TimeSpan.Zero);
        var fixture = CouponEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        await fixture.BuildComposer(Environment).ComposeAsync(
            teamId,
            eventId,
            new WaitlistOfferIntent("WAIT-456", "Conference Pass", expiresAt),
            new CouponEmailDelivery(
                "bob@example.com",
                "bob@example.com",
                "waitlist-coupon-issued:" + teamId.Value + ":" + eventId.Value + ":WAIT-456"),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        var log = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .SingleAsync(testContext.CancellationToken);
        log.EmailType.ShouldBe(BuiltInEmailTemplateNames.WaitlistNotification);
        log.Subject.ShouldBe("Your spot at DevConf is ready — use your coupon");

        var delivery = await Environment.EmailDatabase.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(testContext.CancellationToken);
        var text = delivery.Payload.RootElement.GetProperty("textBody").GetString()!;
        var html = delivery.Payload.RootElement.GetProperty("htmlBody").GetString()!;
        var expiry = expiresAt.ToString("f");

        foreach (var body in new[] { text, html })
        {
            body.ShouldContain("DevConf");
            body.ShouldContain("WAIT-456");
            body.ShouldContain("Conference Pass");
            body.ShouldContain(expiry);
            body.ShouldContain("https://public.example/e/devconf/register");
        }
    }
}
