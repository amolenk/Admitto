using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Sending;
using MailKit.Security;
using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Infrastructure.Email;

public class SmtpEmailSenderFactory(ILoggerFactory loggerFactory) : IEmailSenderFactory
{
    public async ValueTask<IEmailSender> GetEmailSenderAsync(
        string fromName,
        string fromEmail,
        string connectionString)
    {
        var settings = CreateSmtpSettings(fromName, fromEmail, connectionString);
        
        return await SmtpEmailSender.ConnectAsync(settings, loggerFactory);
    }
    
    private static SmtpSettings CreateSmtpSettings(string fromName, string fromEmail, string connectionString)
    {
        var values = connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim().ToLowerInvariant(), p => p[1].Trim());
        
        return new SmtpSettings
        {
            Host = values["host"],
            Port = int.Parse(values["port"]),
            Username = values.GetValueOrDefault("username"),
            Password = values.GetValueOrDefault("password"),
            SecureSocketOptions = SecureSocketOptions.Auto,
            FromName = fromName,
            FromEmail = fromEmail
        };
    }


}