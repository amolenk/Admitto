using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Email.Templating;

/// <summary>
/// Classes that implement this interface provide functionality to load email templates based on the type of email and
/// associated ticketed event.
/// </summary>
public interface IEmailTemplateService
{
    ValueTask<EmailTemplate> LoadEmailTemplateAsync(
        EmailType type,
        Guid teamId,
        Guid ticketedEventId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Default implementation of <see cref="IEmailTemplateService"/> that loads email templates from the database.
/// If no template is found for the specified type and ticketed event, it returns a default template.
/// </summary>
public class EmailTemplateService(IApplicationContext context) : IEmailTemplateService
{
    // TODO - consider caching templates
    public async ValueTask<EmailTemplate> LoadEmailTemplateAsync(
        EmailType type,
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
            EmailType.VerifyEmail => GetDefaultVerifyEmailTemplate(teamId),
            EmailType.Ticket => GetDefaultTicketTemplate(teamId),
            _ => throw new NotSupportedException($"Email type '{type}' is not supported.")
        };
    }

    private static EmailTemplate GetDefaultVerifyEmailTemplate(Guid teamId)
    {
        return EmailTemplate.Create(
            EmailType.VerifyEmail,
            "Verify Your Email",
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
                          Verify Your Email for {{ event_name }}
                        </td>
                      </tr>
                      <tr>
                        <td style="font-size: 16px; color: #2f3138; line-height: 1.6;">
                          <p style="margin-top: 0;">Hi {{ first_name }},</p>
                          <p>Thank you for your interest in attending <strong>{{ event_name }}</strong>!</p>
                          <p>To complete your registration, please enter the 6-digit verification code below on our website:</p>
                          <p style="font-size: 20px; font-weight: bold; background-color: #f0f0f0; padding: 10px 20px; display: inline-block; border-radius: 4px; letter-spacing: 2px;">
                            {{ verification_code }}
                          </p>
                          <p>This helps us confirm your email address.</p>
                          <p>If you didn’t start registration, you can safely ignore this email.</p>
                          <p>Thank you,</p>
                          <p>The {{ event_name }} team</p>
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

    private static EmailTemplate GetDefaultTicketTemplate(Guid teamId)
    {
        return EmailTemplate.Create(
            EmailType.Ticket,
            "Your {{ event_name }} Ticket",
            """
            <!DOCTYPE html>
            <html>

            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <title>Your {{ event_name }} Ticket</title>
            </head>

            <body style="margin: 0; padding: 0; background-color: #f9f9f9; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" border="0" bgcolor="#f9f9f9">
                <tr>
                  <td align="center">
                    <table width="600" cellpadding="0" cellspacing="0" border="0"
                      style="background-color: #ffffff; margin: 20px auto; padding: 20px; border-radius: 6px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;">
                      <tr>
                        <td style="font-size: 24px; font-weight: bold; color: #2f3138; padding-bottom: 10px;">
                          Your {{ event_name }} Ticket
                        </td>
                      </tr>
                      <tr>
                        <td style="font-size: 16px; color: #2f3138; line-height: 1.6;">
                          <p style="margin-top: 0;">Hi {{ first_name }},</p>
                          <p>Your registration has been confirmed. Here's your ticket for <strong>{{ event_name }}</strong>!</p>

                          <div style="text-align: center; margin: 30px 0;">
                            <img src="{{ qrcode_link }}" alt="Your QR Code Ticket"
                              style="width: 200px; height: 200px; border: 2px solid #ddd; border-radius: 10px; padding: 10px; background: #ffffff;" />
                            <p style="margin-top: 10px; font-size: 14px; color: #666;">Show this QR code at the entrance</p>
                          </div>

                          <p>You can present this QR code on your mobile device or bring a printed copy to the event.</p>

                          {{- if tickets.size > 1 || (tickets[0].quantity | string.to_int) > 1 }}
                          <table width="100%" cellpadding="0" cellspacing="0" border="0" style="margin: 30px 0;">
                            <tr>
                              <td style="font-size: 16px; font-weight: bold; color: #2f3138; padding-bottom: 10px;">
                                Ticket Details
                              </td>
                            </tr>
                            <tr>
                              <td>
                                <table width="100%" cellpadding="6" cellspacing="0" border="0" style="border: 1px solid #eee; border-radius: 4px;">
                                  <thead>
                                    <tr style="background-color: #f0f0f0;">
                                      <th align="left" style="font-size: 14px; color: #333; padding: 6px;">Ticket</th>
                                      <th align="left" style="font-size: 14px; color: #333; padding: 6px;">Quantity</th>
                                    </tr>
                                  </thead>
                                  <tbody>
                                    {{ for ticket in tickets }}
                                    <tr>
                                      <td style="font-size: 14px; color: #2f3138; padding: 6px;">{{ ticket.name }}</td>
                                      <td style="font-size: 14px; color: #2f3138; padding: 6px;">{{ ticket.quantity }}</td>
                                    </tr>
                                    {{ end }}
                                  </tbody>
                                </table>
                              </td>
                            </tr>
                          </table>
                          {{- end }}
                          
                          <table width="100%" cellpadding="0" cellspacing="0" border="0" style="margin: 30px 0; padding: 15px 0; border-top: 1px solid #eee; border-bottom: 1px solid #eee;">
                            <tr>
                              <td style="font-size: 14px; color: #0e1b4d; font-weight: bold; padding-bottom: 10px;">
                                Can’t make it?
                              </td>
                            </tr>
                            <tr>
                              <td style="font-size: 16px; color: #2f3138;">
                                Please cancel your registration in advance so someone else can take your spot. Click the link below to cancel:
                                <p style="margin-top: 10px;">
                                <a href="{{ cancel_link }}" style="background-color: #0e1b4d; color: #ffffff; text-decoration: none; padding: 10px 20px; border-radius: 4px; display: inline-block;">
                                    Cancel My Ticket
                                </a>
                                </p>
                                <p style="font-size: 14px; color: #666;">
                                Or use this link: <br>
                                <a href="{{ cancel_link }}" style="color: #0e1b4d;">{{ cancel_link }}</a>
                                </p>
                              </td>
                            </tr>
                          </table>

                          <p>For details on speakers, sessions, and location, visit <a href="https://{{ event_website }}" target="_blank" rel="noopener noreferrer" style="color: #0e1b4d; text-decoration: underline;">{{ event_website }}</a>.</p>

                          <p>We look forward to seeing you there!</p>
                          <p>Best regards,<br>The {{ event_name }} Team</p>
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