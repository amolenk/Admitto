using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.UseCases.Email.SendEmail;
using Amolenk.Admitto.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Scriban;
using Scriban.Runtime;

namespace Amolenk.Admitto.Infrastructure.Email;

public class EmailOutbox(IDomainContext domainContext, IEmailContext context, IMessageOutbox messageOutbox)
    : IEmailOutbox
{
    public async ValueTask EnqueueEmailAsync(string recipientEmail, EmailTemplateId templateId, 
        Dictionary<string, string> templateParameters, TicketedEventId ticketedEventId, bool priority = false,
        CancellationToken cancellationToken = default)
    {
        var ticketedEventInfo = await GetTicketedEventInfoAsync(ticketedEventId, cancellationToken);
        
        // Enrich the template parameters with additional information
        templateParameters["event_name"] = ticketedEventInfo.Name;
        
        var email = new EmailMessage
        {
            RecipientEmail = recipientEmail,
            Subject = "TODO Get from template",
            Body = await RenderBodyAsync(templateId, templateParameters),
            TeamId = ticketedEventInfo.TeamId
        };

        // Persist the e-mail message to the database
        context.EmailMessages.Add(email);
        
        // And add a command to send the e-mail to the recipient
        messageOutbox.Enqueue(new SendEmailCommand(email.Id), priority);
    }

    private static async ValueTask<string> RenderBodyAsync(EmailTemplateId templateId, 
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

    private static Task<Template> LoadTemplateAsync(EmailTemplateId templateId)
    {
        const string templateContent = "<h1>Registration</h1><p>Hi {{ name }}!</p><p>Here's your code: <strong>{{ code }}</strong>";

        var template = Template.Parse(templateContent);
        if (template.HasErrors)
        {
            throw new InvalidOperationException($"Template parsing failed: {string.Join(", ", template.Messages)}");
        }
        
        return Task.FromResult(template);
    }

    private async ValueTask<(Guid TeamId, string Name)> GetTicketedEventInfoAsync(Guid ticketedEventId,
        CancellationToken cancellationToken)
    {
        var ticketedEventInfo = await domainContext.TicketedEvents
            .Where(e => e.Id == ticketedEventId)
            .Select(e => new
            {
                e.Name,
                e.TeamId
                // TODO Add website URL (in fact, use Detail pattern just like with attendee registrations)
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        
        if (ticketedEventInfo is null)
        {
            throw ValidationError.TicketedEvent.NotFound(ticketedEventId);
        }

        return (ticketedEventInfo.TeamId, ticketedEventInfo.Name);
    }
}