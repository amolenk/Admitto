using Amolenk.Admitto.Application.UseCases.Email;
using Amolenk.Admitto.Domain;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Jobs.SendEmail;

public class SendEmailJobHandler(
    IApplicationContext context,
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
            case EmailType.VerifyEmail:
            case EmailType.Ticket:
            case EmailType.RegistrationFailed:
                return GetAttendeeTemplateParametersAsync(dataEntityId, cancellationToken);
            default:
                throw new ArgumentOutOfRangeException(nameof(emailType), emailType, null);
        }
    }
    
    private async ValueTask<Dictionary<string, string>> GetAttendeeTemplateParametersAsync(
        Guid attendeeId,
        CancellationToken cancellationToken)
    {
        var info = await context.Attendees
            .AsNoTracking()
            .Join(
                context.TicketedEvents,
                a => a.TicketedEventId,
                e => e.Id,
                (a, e) => new { Attendee = a, Event = e })
            .Where(joined => joined.Attendee.Id == attendeeId)
            .Select(joined => new
            {
                joined.Event.Name,
                joined.Event.TicketTypes,
                joined.Attendee.Email,
                joined.Attendee.EmailVerification,
                joined.Attendee.FirstName,
                joined.Attendee.LastName,
                joined.Attendee.AdditionalDetails,
                joined.Attendee.Tickets
            })
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (info is null)
        {
            throw new BusinessRuleException(BusinessRuleError.Attendee.NotFound(attendeeId));
        }

        var templateParameters = new Dictionary<string, string>
        {
            ["event_name"] = info.Name,
            ["email"] = info.Email,
            ["verification_code"] = info.EmailVerification?.Code ?? string.Empty,
            ["first_name"] = info.FirstName,
            ["last_name"] = info.LastName
        };

        // TODO Add ticket details to template parameters
        // TODO Add additional details to template parameters

        return templateParameters;
    }
}