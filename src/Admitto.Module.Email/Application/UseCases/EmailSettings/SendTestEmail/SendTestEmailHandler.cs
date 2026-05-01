using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Application.Sending;
using Amolenk.Admitto.Module.Email.Application.Sending.Settings;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Email.Infrastructure.Security;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using EmailSettingsEntity = Amolenk.Admitto.Module.Email.Domain.Entities.EmailSettings;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.SendTestEmail;

internal sealed class SendTestEmailHandler(
    IEmailWriteStore writeStore,
    IProtectedSecret protectedSecret,
    IEmailSender emailSender)
    : ICommandHandler<SendTestEmailCommand>
{
    public async ValueTask HandleAsync(SendTestEmailCommand command, CancellationToken cancellationToken)
    {
        var settings = await writeStore.EmailSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Scope == command.Scope && s.ScopeId == command.ScopeId,
                cancellationToken)
            ?? throw new BusinessRuleViolationException(Errors.SettingsNotConfigured);

        if (!settings.IsValid())
        {
            throw new BusinessRuleViolationException(Errors.IncompleteSettings);
        }

        var effectiveSettings = ToEffectiveSettings(settings);
        if (!effectiveSettings.IsValid())
        {
            throw new BusinessRuleViolationException(Errors.IncompleteSettings);
        }

        var message = new EmailMessage(
            RecipientAddress: command.Recipient.Value,
            RecipientName: command.Recipient.Value,
            Subject: "Admitto SMTP settings test",
            TextBody:
                "This is a test email from Admitto. If you received it, the saved SMTP settings for this scope can send email.",
            HtmlBody:
                "<p>This is a test email from Admitto.</p><p>If you received it, the saved SMTP settings for this scope can send email.</p>");

        try
        {
            await emailSender.SendAsync(effectiveSettings, message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessRuleViolationException(Errors.SendFailed(ex.Message));
        }
    }

    private EffectiveEmailSettings ToEffectiveSettings(EmailSettingsEntity settings)
    {
        var password = settings.ProtectedPassword is null
            ? null
            : protectedSecret.Unprotect(settings.ProtectedPassword.Value.Ciphertext);

        return new EffectiveEmailSettings(
            settings.SmtpHost,
            settings.SmtpPort,
            settings.FromAddress,
            settings.AuthMode,
            settings.Username?.Value,
            password);
    }

    internal static class Errors
    {
        public static readonly Error SettingsNotConfigured = new(
            "email_settings.not_configured",
            "Email settings have not been configured for this scope.");

        public static readonly Error IncompleteSettings = new(
            "email_settings.incomplete",
            "Saved email settings are incomplete.");

        public static Error SendFailed(string message) => new(
            "email_settings.test_send_failed",
            $"Failed to send test email: {message}");
    }
}
