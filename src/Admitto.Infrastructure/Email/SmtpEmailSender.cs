using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Domain.ValueObjects;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Amolenk.Admitto.Infrastructure.Email;

public class SmtpEmailSender(IDomainContext domainContext, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendEmailAsync(string recipientEmail, string subject, string body, TeamId teamId)
    {
        var team = await domainContext.Teams.FirstOrDefaultAsync(t => t.Id == teamId.Value);
        if (team is null)
        {
            logger.LogError("Cannot send e-mail for team {teamId}, because it doesn't exist.", teamId.Value);
            return;
        }
        
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(team.Name, team.EmailSettings.SenderEmail));
        message.To.Add(new MailboxAddress(recipientEmail, recipientEmail));
        message.Subject = subject;

        message.Body = new TextPart("html")
        {
            Text = body
        };
        
        using var client = new SmtpClient();

        await client.ConnectAsync(team.EmailSettings.SmtpServer, team.EmailSettings.SmtpPort);//, MailKit.Security.SecureSocketOptions.StartTls);
//        await client.AuthenticateAsync("smtp-username", "smtp-password");
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}