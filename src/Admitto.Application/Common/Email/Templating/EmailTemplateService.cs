using Amolenk.Admitto.Application.Common.Email.Composing;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Scriban;
using Scriban.Runtime;

namespace Amolenk.Admitto.Application.Common.Email.Templating;

/// <summary>
/// Classes that implement this interface provide functionality to load email templates based on the type of email and
/// associated ticketed event.
/// </summary>
public interface IEmailTemplateService
{
    // TODO Make private
    ValueTask<EmailTemplate> LoadEmailTemplateAsync(
        string type,
        Guid teamId,
        Guid ticketedEventId,
        CancellationToken cancellationToken);

    ValueTask<EmailMessage> RenderEmailMessageAsync(
        string emailType,
        Guid teamId,
        Guid ticketedEventId,
        IEmailParameters templateParameters,
        Guid? participantId = null,
        CancellationToken cancellationToken = default); 
}

/// <summary>
/// Default implementation of <see cref="IEmailTemplateService"/> that loads email templates from the database.
/// If no template is found for the specified type and ticketed event, it returns a default template.
/// </summary>
public class EmailTemplateService(IApplicationContext context) : IEmailTemplateService
{
    public async ValueTask<EmailMessage> RenderEmailMessageAsync(
        string emailType,
        Guid teamId,
        Guid ticketedEventId,
        IEmailParameters templateParameters,
        Guid? participantId = null,
        CancellationToken cancellationToken = default)
    {
        var scriptObject = new ScriptObject();
        scriptObject.Import(templateParameters);

        var templateContext = new TemplateContext();
        templateContext.PushGlobal(scriptObject);

        var emailTemplate = await LoadEmailTemplateAsync(
            emailType,
            teamId,
            ticketedEventId,
            cancellationToken);

        var subject = await RenderTemplateAsync(emailTemplate.Subject, templateContext);
        var textBody = await RenderTemplateAsync(emailTemplate.TextBody, templateContext);
        var htmlBody = await RenderTemplateAsync(emailTemplate.HtmlBody, templateContext);

        return new EmailMessage(
            templateParameters.Recipient,
            subject,
            textBody,
            htmlBody,
            emailType,
            participantId);
    }
    
    // TODO - consider caching templates
    public async ValueTask<EmailTemplate> LoadEmailTemplateAsync(
        string type,
        Guid teamId,
        Guid ticketedEventId,
        CancellationToken cancellationToken)
    {
        var emailTemplate = await context.EmailTemplates
            .Where(t => t.Type == type && t.TeamId == teamId && (t.TicketedEventId == ticketedEventId
                                                                 || t.TicketedEventId == null))
            .OrderByDescending(t => t.TicketedEventId)
            .FirstOrDefaultAsync(cancellationToken);

        if (emailTemplate is not null) return emailTemplate;

        return type switch
        {
            WellKnownEmailType.Canceled => GetDefaultCancelTemplate(teamId),
            WellKnownEmailType.Reconfirm => GetDefaultReconfirmTemplate(teamId),
            WellKnownEmailType.Ticket => GetDefaultTicketTemplate(teamId),
            WellKnownEmailType.VerifyEmail => GetDefaultVerifyEmailTemplate(teamId),
            WellKnownEmailType.VisaLetterDenied => GetDefaultVisaDeniedTemplate(teamId),
            _ => throw new ApplicationRuleException(ApplicationRuleError.EmailTemplate.TemplateNotSupported(type))
        };
    }

    private static EmailTemplate GetDefaultVerifyEmailTemplate(Guid teamId)
    {
        return EmailTemplate.Create(
            WellKnownEmailType.VerifyEmail,
            "Verify Your Email",
            LoadEmbeddedResource("verify.txt"),
            LoadEmbeddedResource("verify.html"),
            teamId);
    }

    private static EmailTemplate GetDefaultTicketTemplate(Guid teamId)
    {
        return EmailTemplate.Create(
            WellKnownEmailType.Ticket,
            "Your {{ event_name }} Ticket",
            LoadEmbeddedResource("ticket.txt"),
            LoadEmbeddedResource("ticket.html"),
            teamId);
    }

    private static EmailTemplate GetDefaultReconfirmTemplate(Guid teamId)
    {
        return EmailTemplate.Create(
            WellKnownEmailType.Reconfirm,
            "One quick click: Reconfirm your {{ event_name }} attendance",
            LoadEmbeddedResource("reconfirm.txt"),
            LoadEmbeddedResource("reconfirm.html"),
            teamId);
    }

    private static EmailTemplate GetDefaultCancelTemplate(Guid teamId)
    {
      return EmailTemplate.Create(
        WellKnownEmailType.Canceled,
        "Your {{ event_name }} Registration Has Been Cancelled",
        LoadEmbeddedResource("canceled.txt"),
        LoadEmbeddedResource("canceled.html"),
        teamId);
    }
    
    private static EmailTemplate GetDefaultVisaDeniedTemplate(Guid teamId)
    {
        return EmailTemplate.Create(
            WellKnownEmailType.VisaLetterDenied,
            "Your {{ event_name }} Registration Has Been Cancelled",
            LoadEmbeddedResource("visa-letter-denied.txt"),
            LoadEmbeddedResource("visa-letter-denied.html"),
            teamId);
    }

    private static string LoadEmbeddedResource(string resourceName)
    {
        var assembly = typeof(EmailTemplateService).Assembly;
        var resourcePath = $"Amolenk.Admitto.Application.Common.Email.Templating.Defaults.{resourceName}";
        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null) throw new InvalidOperationException($"Resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
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