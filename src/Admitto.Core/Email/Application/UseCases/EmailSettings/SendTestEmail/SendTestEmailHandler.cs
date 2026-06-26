using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Infrastructure.Security;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using EmailSettingsEntity = Amolenk.Admitto.Core.Email.Domain.Entities.EmailSettings;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.SendTestEmail;

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
                s => s.TeamId == TeamId.From(command.TeamId),
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

        var accentColor = settings.AccentColor.Value;
        var fontFamily = NormalizePreviewFontFamily(settings.FontFamily.Value);

        var message = new EmailMessage(
            RecipientAddress: command.Recipient,
            RecipientName: command.Recipient,
            Subject: "Admitto SMTP settings test",
            TextBody:
                $"This is a test email from Admitto. If you received it, the saved SMTP settings can send email. Branding preview: accent color {accentColor}, font family {fontFamily}.",
            HtmlBody:
                $$"""
                <!DOCTYPE html>
                <html>
                <body style="margin:0;padding:0;background:#f6f7fb;font-family:{{fontFamily}};color:#111827;">
                    <div style="max-width:560px;margin:24px auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:14px;overflow:hidden;">
                        <div style="height:8px;background:{{accentColor}};"></div>
                        <div style="padding:28px;">
                            <p style="margin:0 0 10px 0;color:{{accentColor}};font-size:12px;font-weight:700;letter-spacing:.12em;text-transform:uppercase;">Admitto test email</p>
                            <h1 style="margin:0 0 14px 0;font-size:28px;line-height:1.15;color:#111827;">Your email branding is active</h1>
                            <p style="margin:0 0 20px 0;font-size:15px;line-height:1.6;color:#4b5563;">If you received this message, the saved SMTP settings can send email. This preview uses the configured team font and accent color.</p>
                            <div style="border:1px solid #e5e7eb;border-radius:10px;padding:16px;margin:20px 0;background:#fafafa;">
                                <div style="font-size:13px;color:#6b7280;margin-bottom:8px;">Branding values</div>
                                <div style="font-size:14px;line-height:1.7;"><strong>Accent color:</strong> <span style="color:{{accentColor}};">{{accentColor}}</span></div>
                                <div style="font-size:14px;line-height:1.7;"><strong>Font family:</strong> {{fontFamily}}</div>
                            </div>
                            <a href="#" style="display:inline-block;background:{{accentColor}};color:#ffffff;text-decoration:none;font-weight:700;padding:11px 18px;border-radius:8px;">Sample action button</a>
                        </div>
                    </div>
                </body>
                </html>
                """);

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
            password,
            settings.AccentColor,
            settings.FontFamily);
    }

    private static string NormalizePreviewFontFamily(string fontFamily)
    {
        if (fontFamily.StartsWith("Roboto", StringComparison.OrdinalIgnoreCase))
            return "'Helvetica Neue', Helvetica, Arial, sans-serif";

        if (fontFamily.StartsWith("Inter", StringComparison.OrdinalIgnoreCase))
            return "-apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif";

        return fontFamily;
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
