namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IEmailProvider
{
    Task SendEmailAsync(Guid ticketedEventId, string recipientEmail, string subject, string body);
}

public class ConsoleEmailProvider : IEmailProvider
{
    public Task SendEmailAsync(Guid ticketedEventId, string recipientEmail, string subject, string body)
    {
        Console.WriteLine($"[Email Sent] To: {recipientEmail}");
        Console.WriteLine($"Subject: {subject}");
        Console.WriteLine($"Body:\n{body}");
        return Task.CompletedTask;
    }
}