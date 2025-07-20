using Amolenk.Admitto.Application.UseCases.Email;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Jobs.SendEmail;

public class SendEmailJobHandler(
    IDomainContext context,
    IEmailTemplateService emailTemplateService,
    IEmailSender emailSender)
    : IJobHandler<SendEmailJobData>
{
    public async ValueTask HandleAsync(
        SendEmailJobData jobData,
        IJobExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        var templateParameters = await GetTemplateParametersAsync(
            jobData.EmailType,
            jobData.DataEntityId,
            cancellationToken);
        
        var (subject, body) = await emailTemplateService.RenderTemplateAsync(
            jobData.EmailType,
            templateParameters,
            jobData.TeamId,
            jobData.TicketedEventId,
            cancellationToken);
        
        await emailSender.SendEmailAsync(
            jobData.RecipientEmail ?? templateParameters["email"],
            subject,
            body,
            jobData.TeamId);
    }

    private ValueTask<Dictionary<string, string>> GetTemplateParametersAsync(
        EmailType emailType,
        Guid dataEntityId,
        CancellationToken cancellationToken)
    {
        switch (emailType)
        {
            case EmailType.VerifyRegistration:
                return GetRegistrationTemplateParametersAsync(dataEntityId, cancellationToken);
            case EmailType.Ticket:
            case EmailType.RegistrationRejected:
            default:
                throw new ArgumentOutOfRangeException(nameof(emailType), emailType, null);
        }
    }
    
    private async ValueTask<Dictionary<string, string>> GetRegistrationTemplateParametersAsync(
        Guid pendingRegistrationId,
        CancellationToken cancellationToken)
    {
        var info = await context.PendingRegistrations
            .AsNoTracking()
            .Join(
                context.TicketedEvents,
                r => r.TicketedEventId,
                e => e.Id,
                (r, e) => new { Registration = r, Event = e })
            .Where(joined => joined.Registration.Id == pendingRegistrationId)
            .Select(joined => new
            {
                joined.Event.Name,
                joined.Event.TicketTypes,
                joined.Registration.Email,
                joined.Registration.FirstName,
                joined.Registration.LastName,
                joined.Registration.AdditionalDetails,
                joined.Registration.Tickets
            })
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (info is null)
        {
            // TODO
            throw new Exception("Registration not found");
        }

        var templateParameters = new Dictionary<string, string>
        {
            ["event_name"] = info.Name,
            ["email"] = info.Email,
            ["first_name"] = info.FirstName,
            ["last_name"] = info.LastName
        };

        // TODO Add ticket details to template parameters
        // TODO Add additional details to template parameters

        return templateParameters;
    }
}