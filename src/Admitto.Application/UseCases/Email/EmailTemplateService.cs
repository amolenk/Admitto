using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Scriban;
using Scriban.Runtime;

namespace Amolenk.Admitto.Application.UseCases.Email;

// TODO Is this the right location? It's also used by the jobs

public interface IEmailTemplateService
{
    ValueTask<(string Subject, string Body)> RenderTemplateAsync(
        EmailType type,
        Dictionary<string, string> templateParameters,
        Guid teamId,
        Guid? ticketedEventId = null,
        CancellationToken cancellationToken = default);
}

public class EmailTemplateService(IApplicationContext context) : IEmailTemplateService
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
            .Where(t => t.Type == type && t.TeamId == teamId && (t.TicketedEventId == ticketedEventId
                                                                 || t.TicketedEventId == null))
            .OrderByDescending(t => t.TicketedEventId)
            .FirstOrDefaultAsync(cancellationToken);

        if (emailTemplate is not null) return emailTemplate;

        return type switch
        {
            EmailType.VerifyEmail => GetDefaultVerifyRegistrationTemplate(teamId),
            _ => throw new NotSupportedException($"Email type '{type}' is not supported.")
        };
    }

    private static EmailTemplate GetDefaultVerifyRegistrationTemplate(Guid teamId)
    {
        return EmailTemplate.Create(
            EmailType.VerifyEmail,
            "Verify Your Email to Complete Registration",
            """
            <!DOCTYPE html>
            <html>
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <title>Verify Your Email</title>
            </head>
            <body style="margin: 0; padding: 0; background-color: #f9f9f9; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" border="0" bgcolor="#f9f9f9">
                <tr>
                  <td align="center">
                    <table width="600" cellpadding="0" cellspacing="0" border="0" style="background-color: #ffffff; margin: 20px auto; padding: 20px; border-radius: 6px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;">
                      <tr>
                        <td style="font-size: 24px; font-weight: bold; color: #2f3138; padding-bottom: 10px;">
                          Confirm Your {{event_name}} Registration
                        </td>
                      </tr>
                      <tr>
                        <td style="font-size: 16px; color: #2f3138; line-height: 1.6;">
                          <p style="margin-top: 0;">Hi {{first_name}},</p>
                          <p>Thank you for your interest in attending <strong>{{event_name}}</strong>!</p>
                          <p>To complete your registration, please enter the 6-digit verification code below on our website:</p>
                          <p style="font-size: 20px; font-weight: bold; background-color: #f0f0f0; padding: 10px 20px; display: inline-block; border-radius: 4px; letter-spacing: 2px;">
                            {{verification_code}}
                          </p>
                          <p>This helps us confirm your email address and secure your account.</p>
                          <p>If you didn’t sign up, you can safely ignore this email.</p>
                          <p>Thank you,</p>
                          <p>The {{event_name}} team</p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """,
            teamId);
    }
}