using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.SendEmail;

[TestClass]
public sealed class SendEmailHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a team with configured email settings
    // When SendEmail is handled
    // Then a pending email log is written and a delivery command is queued in the outbox without sending yet
    [TestMethod]
    public async ValueTask HandleAsync_ValidSettings_WritesPendingLogAndDeliveryCommand()
    {
        // Arrange
        var (teamId, eventId, fakeSender, handler) = await BuildHandlerAsync();

        var command = new SendEmailCommand(
            teamId.Value, eventId.Value,
            "alice@example.com", "Alice",
            BuiltInEmailTemplateNames.TicketConfirmation,
            IdempotencyKey: "test-key-1",
            Parameters: new { FirstName = "Alice", EventName = "DevConf" });

        // Act
        await handler.HandleAsync(command, testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        // Assert
        await Environment.EmailDatabase.AssertAsync(async db =>
        {
            var log = await db.EmailLog
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.IdempotencyKey == "test-key-1", testContext.CancellationToken);

            log.ShouldNotBeNull();
            log.Status.ShouldBe(EmailLogStatus.Pending);
            log.SentAt.ShouldBeNull();
            log.LastError.ShouldBeNull();

            var outboxMessage = await db.OutboxMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Type == "Email:Emails.DeliverEmail.DeliverEmailCommand", testContext.CancellationToken);
            outboxMessage.ShouldNotBeNull();
        });

        fakeSender.SentMessages.ShouldBeEmpty();
    }

    // Given a team with no SMTP settings configured
    // When SendEmail is handled
    // Then a pending claim and delivery command are still prepared
    [TestMethod]
    public async ValueTask HandleAsync_NoSettings_PreparesPendingDelivery()
    {
        // Arrange — no settings seeded
        var (teamId, eventId, fakeSender, handler) = await BuildHandlerAsync(seedTeamBrandingContext: false);

        var command = new SendEmailCommand(
            teamId.Value, eventId.Value,
            "alice@example.com", "Alice",
            BuiltInEmailTemplateNames.TicketConfirmation,
            IdempotencyKey: "test-key-no-settings",
            Parameters: new { FirstName = "Alice", EventName = "DevConf" });

        // Act
        await handler.HandleAsync(command, testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        // Assert
        await Environment.EmailDatabase.AssertAsync(async db =>
        {
            var log = await db.EmailLog
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.IdempotencyKey == "test-key-no-settings", testContext.CancellationToken);

            log.ShouldNotBeNull();
            log.Status.ShouldBe(EmailLogStatus.Pending);
            log.LastError.ShouldBeNull();

            var delivery = await db.OutboxMessages
                .AsNoTracking()
                .SingleOrDefaultAsync(m => m.Type == "Email:Emails.DeliverEmail.DeliverEmailCommand", testContext.CancellationToken);
            delivery.ShouldNotBeNull();
        });

        fakeSender.SentMessages.ShouldBeEmpty();
    }

    // Given a SendEmail command with a given idempotency key
    // When the command is handled twice with the same key
    // Then only one email log row exists and no message is sent
    [TestMethod]
    public async ValueTask HandleAsync_DuplicateIdempotencyKey_DoesNotDoubleSend()
    {
        // Arrange
        var (teamId, eventId, fakeSender, handler) = await BuildHandlerAsync();

        var command = new SendEmailCommand(
            teamId.Value, eventId.Value,
            "alice@example.com", "Alice",
            BuiltInEmailTemplateNames.TicketConfirmation,
            IdempotencyKey: "test-key-dedup",
            Parameters: new { FirstName = "Alice", EventName = "DevConf" });

        // Act — send twice
        await handler.HandleAsync(command, testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await handler.HandleAsync(command, testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        // Assert — only one delivery command was prepared.
        fakeSender.SentMessages.ShouldBeEmpty();

        var logCount = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .CountAsync(l => l.IdempotencyKey == "test-key-dedup", testContext.CancellationToken);
        logCount.ShouldBe(1);
    }

    // Given a pending email log already exists for the idempotency key
    // When SendEmail is handled again
    // Then no message is sent directly but exactly one delivery command is prepared for recovery
    [TestMethod]
    public async ValueTask HandleAsync_PreExistingPendingLog_PreparesDeliveryCommandForRecovery()
    {
        var (teamId, eventId, fakeSender, handler) = await BuildHandlerAsync();
        await SeedLogAsync(teamId, eventId, "test-key-pending-recovery", EmailLogStatus.Pending);

        var command = new SendEmailCommand(
            teamId.Value, eventId.Value,
            "alice@example.com", "Alice",
            BuiltInEmailTemplateNames.TicketConfirmation,
            IdempotencyKey: "test-key-pending-recovery",
            Parameters: new { FirstName = "Alice", EventName = "DevConf" });

        await handler.HandleAsync(command, testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        fakeSender.SentMessages.ShouldBeEmpty();
        var deliveryCommandCount = await Environment.EmailDatabase.Context.OutboxMessages
            .AsNoTracking()
            .CountAsync(m => m.Type == "Email:Emails.DeliverEmail.DeliverEmailCommand", testContext.CancellationToken);
        deliveryCommandCount.ShouldBe(1);
    }

    // Given ticket confirmation template parameters with both an event website and a public event link
    // When the template is rendered
    // Then the rendered content links to the public event link, not the raw event website, and shows edit-registration wording
    [TestMethod]
    public void TicketConfirmationTemplate_EventWebsiteLink_UsesPublicEventLink()
    {
        var template = BuiltInEmailTemplateCatalog.CreateTemplate(BuiltInEmailTemplateNames.TicketConfirmation);
        var parameters = EmailTemplateParameters.WithBranding(
            new
            {
                FirstName = "Alice",
                EventName = "DevConf",
                EventWebsite = "https://devconf.example.com",
                PublicEventLink = "https://admitto.example.com/e/devconf",
                QRCodeLink = "https://admitto.example.com/e/devconf/qr-code/registration-id",
                CancelLink = "https://admitto.example.com/e/devconf/cancel/registration-id",
                EditRegistrationLink = "https://admitto.example.com/e/devconf/edit/registration-id",
                TicketTypes = Array.Empty<string>()
            },
            AccentColor.From("#0f766e"),
            EmailFontFamily.From("Arial"));

        var rendered = new ScribanEmailRenderer().Render(template, parameters);

        rendered.HtmlBody.ShouldContain("href=\"https://admitto.example.com/e/devconf\"");
        rendered.HtmlBody.ShouldContain(">our website</a>");
        rendered.HtmlBody.ShouldContain("Modify/Cancel Registration");
        rendered.HtmlBody.ShouldContain("href=\"https://admitto.example.com/e/devconf/edit/registration-id\"");
        rendered.HtmlBody.ShouldNotContain("Cancel My Registration");
        rendered.HtmlBody.ShouldNotContain("https://devconf.example.com");
        rendered.TextBody.ShouldContain("https://admitto.example.com/e/devconf");
        rendered.TextBody.ShouldContain("Modify/Cancel your Registration");
        rendered.TextBody.ShouldContain("https://admitto.example.com/e/devconf/edit/registration-id");
        rendered.TextBody.ShouldNotContain("Cancel your registration:");
        rendered.TextBody.ShouldNotContain("https://devconf.example.com");
    }

    // Given branding parameters with a configured font family and accent color
    // When every built-in email template is rendered
    // Then each rendered template's HTML includes the configured font family and accent color
    [TestMethod]
    public void BuiltInEmailTemplates_RenderConfiguredFontAndAccentColor()
    {
        var renderer = new ScribanEmailRenderer();
        var parameters = EmailTemplateParameters.WithBranding(
            EmailTemplateSampleParameters.Create(),
            AccentColor.From("#0f766e"),
            EmailFontFamily.From("Georgia, serif"));

        foreach (var entry in BuiltInEmailTemplateCatalog.All)
        {
            var template = BuiltInEmailTemplateCatalog.CreateTemplate(entry.Name);

            var rendered = renderer.Render(template, parameters);

            rendered.HtmlBody.Contains("font-family: Georgia, serif")
                .ShouldBeTrue($"Built-in template '{entry.Name}' must render the configured font family.");
            rendered.HtmlBody.Contains("#0f766e")
                .ShouldBeTrue($"Built-in template '{entry.Name}' must render the configured accent color.");
        }
    }

    // Given branding parameters built with an accent color and font family
    // When the template parameters are constructed
    // Then they expose canonical 'accent_color' and 'font_family' keys and no legacy 'team_accent_color' key
    [TestMethod]
    public void EmailTemplateParameters_AccentColorArgument_ExportsCanonicalAccentColor()
    {
        var parameters = EmailTemplateParameters.WithBranding(
            new { FirstName = "Alice" },
            AccentColor.From("#dc2626"),
            EmailFontFamily.From("Arial"));

        parameters["accent_color"].ShouldBe("#dc2626");
        parameters["font_family"].ShouldBe("Arial");
        parameters.ShouldNotContainKey("team_accent_color");
    }

    private async ValueTask<(TeamId, TicketedEventId, FakeEmailSender, SendEmailHandler)> BuildHandlerAsync(
        bool seedTeamBrandingContext = true)
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fakeSender = new FakeEmailSender();

        if (seedTeamBrandingContext)
            await SeedTeamEmailContextAsync(teamId);

        var templateService = new EmailTemplateService();
        var renderer = new ScribanEmailRenderer();
        var preparationService = new EmailPreparationService(
            Environment.EmailDatabase.Context,
            templateService,
            renderer);
        var outbox = new Outbox(Environment.EmailDatabase.Context);
        var prepareDeliveryHandler = new PrepareEmailDeliveryHandler(
            Environment.EmailDatabase.Context,
            outbox);

        var handler = new SendEmailHandler(
            Environment.EmailDatabase.Context,
            preparationService,
            prepareDeliveryHandler);

        return (teamId, eventId, fakeSender, handler);
    }

    private async ValueTask SeedTeamEmailContextAsync(TeamId teamId)
    {
        var now = DateTimeOffset.UtcNow;
        var teamContext = TeamEmailContextView.Create(
            teamId, "DevConf Team", "#0f766e", teamVersion: 1, now);

        await Environment.EmailDatabase.SeedAsync(db => db.TeamEmailContexts.Add(teamContext));
    }

    private async ValueTask SeedLogAsync(
        TeamId teamId,
        TicketedEventId eventId,
        string idempotencyKey,
        EmailLogStatus status,
        DateTimeOffset? sentAt = null)
    {
        var now = DateTimeOffset.UtcNow;
        await Environment.EmailDatabase.SeedAsync(db => db.EmailLog.Add(EmailLog.Create(
            teamId: teamId,
            ticketedEventId: eventId,
            idempotencyKey: idempotencyKey,
            recipient: EmailAddress.From("alice@example.com"),
            emailType: BuiltInEmailTemplateNames.TicketConfirmation,
            subject: "Subject",
            status: status,
            sentAt: sentAt,
            statusUpdatedAt: now)));
    }

}
