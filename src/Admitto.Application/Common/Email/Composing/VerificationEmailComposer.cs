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

    protected override async ValueTask<IEmailParameters> GetTemplateParametersAsync(
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

        return new VerificationEmailParameters(
            item.Name,
            item.Website,
            item.Email,
            verificationCode);
    }

    protected override IEmailParameters GetTestTemplateParameters(
        string recipient,
        List<AdditionalDetail> additionalDetails,
        List<TicketSelection> tickets)
    {
        return new VerificationEmailParameters(
            "Test Event",
            "www.example.com",
            recipient,
            "123456");
    }
}