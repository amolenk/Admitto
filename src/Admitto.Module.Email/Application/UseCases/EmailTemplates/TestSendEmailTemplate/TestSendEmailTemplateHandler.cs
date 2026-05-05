using Amolenk.Admitto.Module.Email.Application.Sending;
using Amolenk.Admitto.Module.Email.Application.Sending.Settings;
using Amolenk.Admitto.Module.Email.Application.Templating;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate;

internal sealed class TestSendEmailTemplateHandler(
    IEffectiveEmailSettingsResolver settingsResolver,
    IEmailTemplateService templateService,
    IEmailRenderer renderer,
    IEmailSender emailSender)
    : ICommandHandler<TestSendEmailTemplateCommand>
{
    public async ValueTask HandleAsync(TestSendEmailTemplateCommand command, CancellationToken ct)
    {
        var settings = command.EventId.HasValue
            ? await settingsResolver.ResolveAsync(command.TeamId, command.EventId.Value, ct)
            : await settingsResolver.ResolveAsync(command.TeamId, ct);

        if (settings is null || !settings.IsValid())
            throw new BusinessRuleViolationException(Errors.SettingsNotConfigured);

        EmailTemplate template;
        try
        {
            template = command.EventId.HasValue
                ? await templateService.LoadAsync(command.Type, command.TeamId, command.EventId.Value, ct)
                : await templateService.LoadAsync(command.Type, command.TeamId, ct);
        }
        catch (InvalidOperationException)
        {
            throw new BusinessRuleViolationException(Errors.TemplateNotAvailable);
        }

        var parameters = EmailTemplateSampleParameters.Create();

        RenderedEmail rendered;
        try
        {
            rendered = renderer.Render(template, parameters);
        }
        catch (EmailRenderException ex)
        {
            throw new BusinessRuleViolationException(new Error("email_template.render_failed", ex.Message));
        }

        var message = new EmailMessage(
            RecipientAddress: command.Recipient.Value,
            RecipientName: command.Recipient.Value,
            Subject: rendered.Subject,
            TextBody: rendered.TextBody,
            HtmlBody: rendered.HtmlBody);

        try
        {
            await emailSender.SendAsync(settings, message, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessRuleViolationException(Errors.SendFailed(ex.Message));
        }
    }

    internal static class Errors
    {
        public static readonly Error SettingsNotConfigured = new(
            "email_settings.not_configured",
            "Email settings have not been configured for this scope.");

        public static readonly Error TemplateNotAvailable = new(
            "email_template.not_available",
            "No template is available for this type. Configure a custom template first.");

        public static Error SendFailed(string message) => new(
            "email_template.test_send_failed",
            $"Failed to send test email: {message}");
    }
}
