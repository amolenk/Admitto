using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeVerificationCode;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.ComposeVerificationCode;

[TestClass]
public sealed class VerificationCodeEmailComposerTests(TestContext testContext)
    : AspireIntegrationTestBase
{
    // Given a complete event projection and no team branding projection
    // When a verification code email is composed
    // Then the built-in email contains the code, event, default team label, and default accent in text and HTML
    [TestMethod]
    public async ValueTask ComposeAsync_CompleteEventContext_UsesEventScopeAndDefaultBranding()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = VerificationCodeEmailComposerFixture.CompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        await fixture.BuildComposer(Environment).ComposeAsync(
            teamId,
            eventId,
            new VerificationCodeIntent("123456"),
            new VerificationCodeDelivery(
                "alice@example.com",
                "alice@example.com",
                "otp-requested:code-1"),
            testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        var log = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .SingleAsync(testContext.CancellationToken);
        log.EmailType.ShouldBe(BuiltInEmailTemplateNames.VerificationCode);
        log.Subject.ShouldBe("Your DevConf registration code");
        log.Recipient.Value.ShouldBe("alice@example.com");
        log.IdempotencyKey.ShouldBe("otp-requested:code-1");

        var delivery = await Environment.EmailDatabase.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(testContext.CancellationToken);
        var text = delivery.Payload.RootElement.GetProperty("textBody").GetString()!;
        var html = delivery.Payload.RootElement.GetProperty("htmlBody").GetString()!;

        foreach (var body in new[] { text, html })
        {
            body.ShouldContain("123456");
            body.ShouldContain("DevConf");
            body.ShouldContain("Admitto");
        }

        html.ShouldContain("#2563eb");
    }

    // Given an incomplete event projection
    // When a verification code email is composed
    // Then it fails before creating an email claim or delivery message
    [TestMethod]
    public async ValueTask ComposeAsync_IncompleteEventContext_LeavesNoDeliveryClaim()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fixture = VerificationCodeEmailComposerFixture.IncompleteEventContext();
        await fixture.SetupAsync(Environment, teamId, eventId);

        await Should.ThrowAsync<EventEmailContextMissingException>(async () =>
            await fixture.BuildComposer(Environment).ComposeAsync(
                teamId,
                eventId,
                new VerificationCodeIntent("123456"),
                new VerificationCodeDelivery(
                    "alice@example.com",
                    "alice@example.com",
                    "otp-requested:code-2"),
                testContext.CancellationToken));

        (await Environment.EmailDatabase.Context.EmailLog.CountAsync(testContext.CancellationToken))
            .ShouldBe(0);
        (await Environment.EmailDatabase.Context.OutboxMessages.CountAsync(testContext.CancellationToken))
            .ShouldBe(0);
    }

    // Given no event projection
    // When a verification code email is composed
    // Then it fails before creating an email claim or delivery message
    [TestMethod]
    public async ValueTask ComposeAsync_MissingEventContext_LeavesNoDeliveryClaim()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();

        await Should.ThrowAsync<EventEmailContextMissingException>(async () =>
            await VerificationCodeEmailComposerFixture
                .CompleteEventContext()
                .BuildComposer(Environment)
                .ComposeAsync(
                    teamId,
                    eventId,
                    new VerificationCodeIntent("123456"),
                    new VerificationCodeDelivery(
                        "alice@example.com",
                        "alice@example.com",
                        "otp-requested:code-3"),
                    testContext.CancellationToken));

        (await Environment.EmailDatabase.Context.EmailLog.CountAsync(testContext.CancellationToken))
            .ShouldBe(0);
        (await Environment.EmailDatabase.Context.OutboxMessages.CountAsync(testContext.CancellationToken))
            .ShouldBe(0);
    }
}
