using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Scriban;
using Scriban.Runtime;

namespace Amolenk.Admitto.Application.UseCases.Email;

public interface IEmailTemplateService
{
    ValueTask<(string Subject, string Body)> RenderTemplateAsync(
        EmailType type,
        Dictionary<string, string> templateParameters,
        Guid teamId,
        Guid? ticketedEventId = null,
        CancellationToken cancellationToken = default);
}

public class EmailTemplateService(IDomainContext context) : IEmailTemplateService
{
    public async ValueTask<(string Subject, string Body)> RenderTemplateAsync(
        EmailType type,
        Dictionary<string, string> templateParameters,
        Guid teamId,
        Guid? ticketedEventId = null,
        CancellationToken cancellationToken = default)
    {
        var scriptObject = new ScriptObject();
        foreach (var templateParameter in templateParameters)
        {
            scriptObject.Add(templateParameter.Key, templateParameter.Value);
        }

        var templateContext = new TemplateContext();
        templateContext.PushGlobal(scriptObject);

        var emailTemplate = await LoadEmailTemplateAsync(type, teamId, ticketedEventId, cancellationToken);
        
        var subject = await RenderTemplateAsync(emailTemplate.Subject, templateContext);
        var body = await RenderTemplateAsync(emailTemplate.Body, templateContext);

        return (subject, body);
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

    // TODO - consider caching templates
    private async ValueTask<EmailTemplate> LoadEmailTemplateAsync(
        EmailType type,
        Guid teamId,
        Guid? ticketedEventId,
        CancellationToken cancellationToken)
    {
        var emailTemplate = await context.EmailTemplates
            .Where(t => t.Type == type && t.TeamId == teamId && t.TicketedEventId == ticketedEventId)
            .OrderByDescending(t => t.TicketedEventId)
            .FirstOrDefaultAsync(cancellationToken);

        if (emailTemplate is not null) return emailTemplate;

        return EmailTemplate.Create(
            type,
            $"Default subject for {type}",
            "<h1>Registration</h1><p>Hi {{ attendee_first_name }}!</p><p>Thanks for registering for {{ event_name }}</p>",
            teamId,
            ticketedEventId);
    }
}