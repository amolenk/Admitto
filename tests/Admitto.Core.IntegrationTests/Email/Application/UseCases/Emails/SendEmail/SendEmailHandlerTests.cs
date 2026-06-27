using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Application.Templating;
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
        var (teamId, eventId, fakeSender, handler) = BuildHandler();

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
        var (teamId, eventId, _, handler) = BuildHandler(configureSystemEmail: false);

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
        var (teamId, eventId, fakeSender, handler) = BuildHandler();

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
        var (teamId, eventId, fakeSender, handler) = BuildHandler();
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
        var (teamId, eventId, fakeSender, handler) = BuildDeliverHandler();
        await SeedLogAsync(teamId, eventId, "sent-key", EmailLogStatus.Sent, sentAt: DateTimeOffset.UtcNow);

        await handler.HandleAsync(DeliverCommand(teamId, eventId, "sent-key"), testContext.CancellationToken);
        await Environment.EmailDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        fakeSender.SendAttempts.ShouldBe(0);
    }

    [TestMethod]
    public async ValueTask DeliverEmail_PendingLogExists_SendsAndMarksSent()
    {
        var (teamId, eventId, fakeSender, handler) = BuildDeliverHandler();
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
        var (teamId, eventId, fakeSender, handler) = BuildDeliverHandler(
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

    private (TeamId, TicketedEventId, FakeEmailSender, SendEmailHandler) BuildHandler(bool configureSystemEmail = true)
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fakeSender = new FakeEmailSender();

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

    private (TeamId, TicketedEventId, FakeEmailSender, DeliverEmailHandler) BuildDeliverHandler(
        EmailDeliveryOptions? deliveryOptions = null)
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var fakeSender = new FakeEmailSender();
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

    private static EffectiveEmailSettingsResolver BuildSettingsResolver(bool configureSystemEmail = true) =>
        new(new SystemEmailSettingsResolver(Options.Create(configureSystemEmail
            ? new SystemEmailOptions
            {
                SmtpHost = "smtp.example.com",
                SmtpPort = 587,
                FromAddress = "tickets@admitto.org",
                AuthMode = "None"
            }
            : new SystemEmailOptions())));

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
