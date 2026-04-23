using Amolenk.Admitto.Module.Email.Application.Settings;
using Amolenk.Admitto.Module.Email.Application.Templating;
using Amolenk.Admitto.Module.Email.Application.UseCases.SendEmail;
using Amolenk.Admitto.Module.Email.Domain.Tests.Builders;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Email.Infrastructure.Security;
using Amolenk.Admitto.Module.Email.Tests.Application.Infrastructure;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Tests.Application.UseCases.SendEmail;

[TestClass]
public sealed class SendEmailCommandHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask HandleAsync_ValidSettings_WritesSentLog()
    {
        // Arrange
        var (teamId, eventId, protectedSecret, fakeSender, handler) = BuildHandler();

        var settings = new EventEmailSettingsBuilder()
            .ForEvent(eventId)
            .WithBasicAuth(protectedPassword: protectedSecret.Protect("pass"))
            .Build();
        await Environment.Database.SeedAsync(db => db.EmailSettings.Add(settings));

        var command = new SendEmailCommand(
            teamId, eventId,
            "alice@example.com", "Alice",
            EmailTemplateType.Ticket,
            IdempotencyKey: "test-key-1",
            Parameters: new { FirstName = "Alice", EventName = "DevConf" });

        // Act
        await handler.HandleAsync(command, testContext.CancellationToken);
        await Environment.Database.Context.SaveChangesAsync(testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async db =>
        {
            var log = await db.EmailLog
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.IdempotencyKey == "test-key-1", testContext.CancellationToken);

            log.ShouldNotBeNull();
            log.Status.ShouldBe(EmailLogStatus.Sent);
            log.SentAt.ShouldNotBeNull();
            log.LastError.ShouldBeNull();
        });

        fakeSender.SentMessages.Count.ShouldBe(1);
    }

    [TestMethod]
    public async ValueTask HandleAsync_NoSettings_WritesFailedLog()
    {
        // Arrange — no settings seeded
        var (teamId, eventId, _, _, handler) = BuildHandler();

        var command = new SendEmailCommand(
            teamId, eventId,
            "alice@example.com", "Alice",
            EmailTemplateType.Ticket,
            IdempotencyKey: "test-key-no-settings",
            Parameters: new { FirstName = "Alice", EventName = "DevConf" });

        // Act
        await handler.HandleAsync(command, testContext.CancellationToken);
        await Environment.Database.Context.SaveChangesAsync(testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async db =>
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
    public async ValueTask HandleAsync_RenderError_WritesFailedLog()
    {
        // Arrange — a broken template
        var (teamId, eventId, protectedSecret, fakeSender, handler) = BuildHandler();

        var settings = new EventEmailSettingsBuilder()
            .ForEvent(eventId)
            .Build();
        var brokenTemplate = new EmailTemplateBuilder()
            .ForEvent(eventId)
            .WithSubject("{{ for }}")
            .Build();
        await Environment.Database.SeedAsync(db =>
        {
            db.EmailSettings.Add(settings);
            db.EmailTemplates.Add(brokenTemplate);
        });

        var command = new SendEmailCommand(
            teamId, eventId,
            "alice@example.com", "Alice",
            EmailTemplateType.Ticket,
            IdempotencyKey: "test-key-render-error",
            Parameters: new { });

        // Act
        await handler.HandleAsync(command, testContext.CancellationToken);
        await Environment.Database.Context.SaveChangesAsync(testContext.CancellationToken);

        // Assert — failed log, nothing sent
        await Environment.Database.AssertAsync(async db =>
        {
            var log = await db.EmailLog
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.IdempotencyKey == "test-key-render-error", testContext.CancellationToken);

            log.ShouldNotBeNull();
            log.Status.ShouldBe(EmailLogStatus.Failed);
        });

        fakeSender.SentMessages.ShouldBeEmpty();
    }

    [TestMethod]
    public async ValueTask HandleAsync_DuplicateIdempotencyKey_DoesNotDoubleSend()
    {
        // Arrange
        var (teamId, eventId, protectedSecret, fakeSender, handler) = BuildHandler();

        var settings = new EventEmailSettingsBuilder()
            .ForEvent(eventId)
            .WithBasicAuth(protectedPassword: protectedSecret.Protect("pass"))
            .Build();
        await Environment.Database.SeedAsync(db => db.EmailSettings.Add(settings));

        var command = new SendEmailCommand(
            teamId, eventId,
            "alice@example.com", "Alice",
            EmailTemplateType.Ticket,
            IdempotencyKey: "test-key-dedup",
            Parameters: new { FirstName = "Alice", EventName = "DevConf" });

        // Act — send twice
        await handler.HandleAsync(command, testContext.CancellationToken);
        await Environment.Database.Context.SaveChangesAsync(testContext.CancellationToken);

        await handler.HandleAsync(command, testContext.CancellationToken);
        await Environment.Database.Context.SaveChangesAsync(testContext.CancellationToken);

        // Assert — only one email sent
        fakeSender.SentMessages.Count.ShouldBe(1);

        var logCount = await Environment.Database.Context.EmailLog
            .AsNoTracking()
            .CountAsync(l => l.IdempotencyKey == "test-key-dedup", testContext.CancellationToken);
        logCount.ShouldBe(1);
    }

    private (TeamId, TicketedEventId, IProtectedSecret, FakeEmailSender, SendEmailCommandHandler) BuildHandler()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var protectedSecret = TestProtectedSecretFactory.Create();
        var fakeSender = new FakeEmailSender();

        var settingsResolver = new EffectiveEmailSettingsResolver(Environment.Database.Context, protectedSecret);
        var templateService = new EmailTemplateService(Environment.Database.Context);
        var renderer = new ScribanEmailRenderer();

        var handler = new SendEmailCommandHandler(
            Environment.Database.Context,
            settingsResolver,
            templateService,
            renderer,
            fakeSender);

        return (teamId, eventId, protectedSecret, fakeSender, handler);
    }
}
