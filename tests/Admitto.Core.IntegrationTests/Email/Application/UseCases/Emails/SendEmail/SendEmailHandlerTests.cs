using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Projections.TeamEmailContext;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.DeliverEmail;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.SendEmail;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.SendEmail;

[TestClass]
public sealed class SendEmailHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
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

    [TestMethod]
    public async ValueTask HandleAsync_NoSettings_WritesFailedLog()
    {
        // Arrange — no settings seeded
        var (teamId, eventId, _, handler) = await BuildHandlerAsync(configureSystemEmail: false);

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
            log.Status.ShouldBe(EmailLogStatus.Failed);
            log.LastError.ShouldNotBeNullOrEmpty();
        });
    }

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

    [TestMethod]
    public async ValueTask DeliverEmail_SentLogExists_DoesNotSendAgain()
    {
        var (teamId, eventId, fakeSender, handler) = await BuildDeliverHandlerAsync();
        await SeedLogAsync(teamId, eventId, "sent-key", EmailLogStatus.Sent, sentAt: DateTimeOffset.UtcNow);

        await handler.HandleAsync(DeliverCommand(teamId, eventId, "sent-key"), testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        fakeSender.SendAttempts.ShouldBe(0);
    }

    [TestMethod]
    public async ValueTask DeliverEmail_PendingLogExists_SendsAndMarksSent()
    {
        var (teamId, eventId, fakeSender, handler) = await BuildDeliverHandlerAsync();
        await SeedLogAsync(teamId, eventId, "deliver-key", EmailLogStatus.Pending);

        await handler.HandleAsync(DeliverCommand(teamId, eventId, "deliver-key"), testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        fakeSender.SendAttempts.ShouldBe(1);
        var log = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .SingleAsync(l => l.IdempotencyKey == "deliver-key", testContext.CancellationToken);
        log.Status.ShouldBe(EmailLogStatus.Sent);
    }

    [TestMethod]
    public async ValueTask DeliverEmail_TransientSmtpFailure_RetriesInlineAndRequeues()
    {
        var (teamId, eventId, fakeSender, handler) = await BuildDeliverHandlerAsync(
            new EmailDeliveryOptions
            {
                InlineRetryCount = 2,
                InlineRetryDelay = TimeSpan.Zero,
                MaxDeliveryAttempts = 3
            });
        fakeSender.ShouldThrow = true;
        await SeedLogAsync(teamId, eventId, "retry-key", EmailLogStatus.Pending);

        await handler.HandleAsync(DeliverCommand(teamId, eventId, "retry-key"), testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        fakeSender.SendAttempts.ShouldBe(3);
        var log = await Environment.EmailDatabase.Context.EmailLog
            .AsNoTracking()
            .SingleAsync(l => l.IdempotencyKey == "retry-key", testContext.CancellationToken);
        log.Status.ShouldBe(EmailLogStatus.Pending);
        log.DeliveryAttemptCount.ShouldBe(1);
        log.LastError.ShouldBe("SMTP error (fake)");

        var requeued = await Environment.EmailDatabase.Context.OutboxMessages
            .AsNoTracking()
            .AnyAsync(m => m.Type == "Email:Emails.DeliverEmail.DeliverEmailCommand", testContext.CancellationToken);
        requeued.ShouldBeTrue();
    }

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
        bool configureSystemEmail = true)
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fakeSender = new FakeEmailSender();

        if (configureSystemEmail)
            await SeedTeamEmailContextAsync(teamId);

        var settingsResolver = BuildSettingsResolver(configureSystemEmail);
        var templateService = new EmailTemplateService();
        var renderer = new ScribanEmailRenderer();
        var outbox = new Outbox(Environment.EmailDatabase.Context);

        var handler = new SendEmailHandler(
            Environment.EmailDatabase.Context,
            settingsResolver,
            templateService,
            renderer,
            outbox);

        return (teamId, eventId, fakeSender, handler);
    }

    [TestMethod]
    public async ValueTask ResolveAsync_NoTeamContextRow_UsesDefaultBrandingAndSystemSenderLabel()
    {
        // The projection is eventually consistent: the team's branding integration event
        // has not reached Email yet, so there is no row at all. Sending must still work.
        var settings = await BuildSettingsResolver().ResolveAsync(TeamId.New(), testContext.CancellationToken);

        settings.ShouldNotBeNull();
        settings.AccentColor.ShouldBe(AccentColor.From(AccentColor.Default));
        settings.FontFamily.ShouldBe(EmailFontFamily.From(EmailFontFamily.Default));
        settings.FromDisplayName.ShouldBe("Admitto");
    }

    private async ValueTask<(TeamId, TicketedEventId, FakeEmailSender, DeliverEmailHandler)> BuildDeliverHandlerAsync(
        EmailDeliveryOptions? deliveryOptions = null)
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fakeSender = new FakeEmailSender();

        await SeedTeamEmailContextAsync(teamId);

        var settingsResolver = BuildSettingsResolver();
        var outbox = new Outbox(Environment.EmailDatabase.Context);
        var options = new StaticOptionsMonitor<EmailDeliveryOptions>(deliveryOptions ?? new EmailDeliveryOptions());

        var handler = new DeliverEmailHandler(
            Environment.EmailDatabase.Context,
            settingsResolver,
            fakeSender,
            outbox,
            options);

        return (teamId, eventId, fakeSender, handler);
    }

    private EffectiveEmailSettingsResolver BuildSettingsResolver(bool configureSystemEmail = true) =>
        new(Options.Create(configureSystemEmail
            ? new SystemEmailOptions
            {
                SmtpHost = "smtp.example.com",
                SmtpPort = 587,
                FromAddress = "tickets@admitto.org",
                AuthMode = "None"
            }
            : new SystemEmailOptions()),
            Environment.EmailDatabase.Context);

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

    private static DeliverEmailCommand DeliverCommand(TeamId teamId, TicketedEventId eventId, string idempotencyKey) =>
        new(
            teamId.Value,
            eventId.Value,
            "alice@example.com",
            "Alice",
            BuiltInEmailTemplateNames.TicketConfirmation,
            idempotencyKey,
            "Subject",
            "Text",
            "<p>Html</p>");

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
