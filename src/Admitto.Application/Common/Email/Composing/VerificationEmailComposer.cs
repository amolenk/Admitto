using Amolenk.Admitto.Application.Common.Email.Templating;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Email.Composing;

/// <summary>
/// Represents a composer for verification emails.
/// </summary>
public class VerificationEmailComposer(
    IApplicationContext context,
    IEmailTemplateService templateService)
    : EmailComposer(templateService)
{
    public const string VerificationCodeParameterName = "verification_code";

    protected override async ValueTask<(IEmailParameters Parameters, Guid? ParticipantId)> GetTemplateParametersAsync(
        Guid ticketedEventId,
        Guid entityId,
        Dictionary<string, string> additionalParameters,
        CancellationToken cancellationToken)
    {
        if (!additionalParameters.TryGetValue(VerificationCodeParameterName, out var verificationCode))
        {
            throw new ApplicationRuleException(
                ApplicationRuleError.EmailVerificationRequest.VerificationCodeParameterMissing);
        }

        var item = await context.EmailVerificationRequests
            .AsNoTracking()
            .Join(
                context.TicketedEvents,
                r => r.TicketedEventId,
                e => e.Id,
                (r, e) => new { Request = r, Event = e })
            .Where(x => x.Request.Id == entityId)
            .Select(r => new
            {
                r.Request.Email,
                r.Event.Name,
                r.Event.Website
            })
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (item is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.EmailVerificationRequest.NotFound(entityId));
        }

        var parameters = new VerificationEmailParameters(
            item.Email,
            item.Name,
            item.Website,
            verificationCode);
        
        return (parameters, null);
    }

    protected override async ValueTask<IEmailParameters> GetTestTemplateParametersAsync(
        Guid ticketedEventId,
        string recipient,
        List<AdditionalDetail> additionalDetails,
        List<TicketSelection> tickets, 
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await context.TicketedEvents
            .AsNoTracking()
            .Where(x => x.Id == ticketedEventId)
            .Select(x => new
            {
                x.Name,
                x.Website
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }
        
        return new VerificationEmailParameters(
            recipient,
            ticketedEvent.Name,
            ticketedEvent.Website,
            "123456");
    }
}