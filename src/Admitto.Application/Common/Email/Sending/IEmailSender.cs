using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Email.Sending;

/// <summary>
/// Classes that implement this interface can send emails.
/// </summary>
public interface IEmailSender : IDisposable
{
    ValueTask SendEmailAsync(EmailMessage emailMessage);
}