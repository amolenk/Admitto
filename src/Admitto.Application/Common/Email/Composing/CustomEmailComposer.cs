using Amolenk.Admitto.Application.Common.Email.Templating;
using Amolenk.Admitto.Domain.ValueObjects;
using Scriban;
using Scriban.Runtime;

namespace Amolenk.Admitto.Application.Common.Email.Composing;

public class CustomEmailComposer(IEmailTemplateService templateService)
{
    public async ValueTask<EmailMessage> ComposeMessageAsync(
        Guid teamId,
        Guid ticketedEventId,
        string emailType,
        string recipient,
        Dictionary<string, object> templateParameters,
        Guid? participantId = null,
        CancellationToken cancellationToken = default)
    {
        var scriptObject = new ScriptObject();
        scriptObject.Import(templateParameters);

        var templateContext = new TemplateContext();
        templateContext.PushGlobal(scriptObject);

        var emailTemplate = await templateService.LoadEmailTemplateAsync(
            emailType,
            teamId,
            ticketedEventId,
            cancellationToken);

        var subject = await RenderTemplateAsync(emailTemplate.Subject, templateContext);
        var textBody = await RenderTemplateAsync(emailTemplate.TextBody, templateContext);
        var htmlBody = await RenderTemplateAsync(emailTemplate.HtmlBody, templateContext);

        return new EmailMessage(
            recipient,
            subject,
            textBody,
            htmlBody,
            emailType,
            participantId);
    }

    private static async ValueTask<string> RenderTemplateAsync(string templateContent, TemplateContext templateContext)
    {
        var template = Template.Parse(templateContent);
        if (template.HasErrors)
        {
            throw new InvalidOperationException($"Template parsing failed: {string.Join(", ", template.Messages)}");
        }

        return await template.RenderAsync(templateContext);
    }
}