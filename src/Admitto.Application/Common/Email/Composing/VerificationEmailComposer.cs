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
        Guid entityId,
        Dictionary<string, string> additionalParameters,
        CancellationToken cancellationToken)
    {
        if (!additionalParameters.TryGetValue(VerificationCodeParameterName, out var verificationCode))
        {
            throw new ApplicationRuleException(
                ApplicationRuleError.EmailVerificationRequest.VerificationCodeParameterMissing);
        }

        var info = await context.EmailVerificationRequests
            .AsNoTracking()
            .Where(r => r.Id == entityId)
            .Select(r => new
            {
                r.Email
            })
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (info is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.EmailVerificationRequest.NotFound(entityId));
        }

        return new VerificationEmailParameters(verificationCode, info.Email);
    }

    protected override IEmailParameters GetTestTemplateParameters(
        string recipient,
        List<AdditionalDetail> additionalDetails,
        List<TicketSelection> tickets)
    {
        return new VerificationEmailParameters("123456", recipient);
    }
}