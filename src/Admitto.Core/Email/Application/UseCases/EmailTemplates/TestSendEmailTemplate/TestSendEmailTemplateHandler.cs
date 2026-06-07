using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate;

internal sealed class TestSendEmailTemplateHandler(
    IEmailWriteStore writeStore,
    IEffectiveEmailSettingsResolver settingsResolver,
    IEmailRenderer renderer,
    IEmailSender emailSender)
    : ICommandHandler<TestSendEmailTemplateCommand>
{
    public async ValueTask HandleAsync(TestSendEmailTemplateCommand command, CancellationToken ct)
    {
        EmailTemplateId templateId = EmailTemplateId.From(command.TemplateId);
        TeamId teamId = TeamId.From(command.TeamId);
        TicketedEventId? eventId = command.EventId.HasValue
            ? TicketedEventId.From(command.EventId.Value)
            : null;

        var settings = eventId.HasValue
            ? await settingsResolver.ResolveAsync(teamId, eventId.Value, ct)
            : await settingsResolver.ResolveAsync(teamId, ct);

        if (settings is null || !settings.IsValid())
            throw new BusinessRuleViolationException(Errors.SettingsNotConfigured);

        var template = await writeStore.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId &&
                                      t.TeamId == teamId &&
                                      t.TicketedEventId == eventId, ct)
            ?? throw new BusinessRuleViolationException(Errors.TemplateNotFound);

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
            RecipientAddress: command.Recipient,
            RecipientName: command.Recipient,
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

        public static readonly Error TemplateNotFound = new(
            "email_template.not_found",
            "The specified email template was not found.");

        public static Error SendFailed(string message) => new(
            "email_template.test_send_failed",
            $"Failed to send test email: {message}");
    }
}
