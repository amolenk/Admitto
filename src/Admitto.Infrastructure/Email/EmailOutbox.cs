using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.UseCases.Email.SendEmail;
using Amolenk.Admitto.Domain.ValueObjects;
using Scriban;
using Scriban.Runtime;

namespace Amolenk.Admitto.Infrastructure.Email;

public class EmailOutbox(IEmailContext context, IMessageOutbox messageOutbox) : IEmailOutbox
{
    public async ValueTask EnqueueEmailAsync(string recipientEmail, string subject, string templateId, 
        Dictionary<string, string> templateParameters, TeamId teamId, TicketedEventId? ticketedEventId = null,
        AttendeeId? attendeeId = null, bool priority = false)
    {
        var email = new EmailMessage
        {
            RecipientEmail = recipientEmail,
            Subject = subject,
            Body = await RenderBodyAsync(templateId, templateParameters),
            TeamId = teamId
        };

        // Persist the e-mail message to the database
        context.EmailMessages.Add(email);
        
        // And add a command to send the e-mail to the recipient
        messageOutbox.Enqueue(new SendEmailCommand(email.Id), priority);
    }

    private static async ValueTask<string> RenderBodyAsync(string templateId, 
        Dictionary<string, string> templateParameters)
    {
        var scriptObject = new ScriptObject();
        foreach (var templateParameter in templateParameters)
        {
            scriptObject.Add(templateParameter.Key, templateParameter.Value);
        }
        
        var templateContext = new TemplateContext();
        templateContext.PushGlobal(scriptObject);

        var template = await LoadTemplateAsync(templateId);
        
        return await template.RenderAsync(templateContext);
    }

    private static Task<Template> LoadTemplateAsync(string templateId)
    {
        const string templateContent = "<h1>Registration</h1><p>Hi {{ name }}!</p><p>Here's your code: <strong>{{ code }}</strong>";

        var template = Template.Parse(templateContent);
        if (template.HasErrors)
        {
            throw new InvalidOperationException($"Template parsing failed: {string.Join(", ", template.Messages)}");
        }
        
        return Task.FromResult(template);
    }
}