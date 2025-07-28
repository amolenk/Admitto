namespace Amolenk.Admitto.Application.Common.Email.Sending;

/// <summary>
/// Classes that implement this interface can create instances of <see cref="IEmailSender"/>.
/// </summary>
public interface IEmailSenderFactory
{
    public ValueTask<IEmailSender> GetEmailSenderAsync(
        string fromName,
        string fromEmail,
        string connectionString);
}