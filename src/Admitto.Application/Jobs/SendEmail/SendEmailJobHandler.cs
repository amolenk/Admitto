using Amolenk.Admitto.Domain.ValueObjects;
using Scriban;
using Scriban.Runtime;

namespace Amolenk.Admitto.Application.Jobs.SendEmail;

public class SendEmailJobHandler(IDomainContext domainContext, IEmailSender emailSender, ILogger<SendEmailJobHandler> logger) : IJobHandler<SendEmailJobData>
{
    public async ValueTask HandleAsync(SendEmailJobData jobData, IJobExecutionContext executionContext, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();

        // TODO Check that we haven't send yet

        // await executionContext.ReportProgressAsync("Starting email send", 0, cancellationToken);

        // var ticketedEventInfo = await GetTicketedEventInfoAsync(jobData.TicketedEventId, cancellationToken);


        // return registration.Details.ToDictionary(
        //         x => $"attendee_detail_{x.Name}", x => x.Value)
        //     .Concat(new Dictionary<string, string>
        //     {
        //         ["attendee_first_name"] = registration.FirstName,
        //         ["attendee_last_name"] = registration.LastName
        //     })
        //     .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);



        //
        // // Enrich the template parameters with additional information
        // var templateParameters = jobData.TemplateParameters;
        // templateParameters["event_name"] = ticketedEventInfo.Name;
        //
        // var subject = "TODO Get subject from template"; // TODO: Get subject from template
        // var body = await RenderBodyAsync(jobData.TemplateId, templateParameters);
        //
        // await emailSender.SendEmailAsync(
        //     jobData.RecipientEmail,
        //     subject,
        //     body,
        //     ticketedEventInfo.TeamId);

        // TODO Report progress after sending the email
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