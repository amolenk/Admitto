namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail;

/// <summary>
/// Sends an e-mail.
/// </summary>
public class SendEmailHandler(IEmailContext context, IEmailSender emailProvider, ILogger<SendEmailHandler> logger)
    : ICommandHandler<SendEmailCommand>
{
    public async ValueTask HandleAsync(SendEmailCommand command, CancellationToken cancellationToken)
    {
        var email = await context.EmailMessages.FindAsync([command.EmailId], cancellationToken);
        if (email is null)
        {
            logger.LogError("Cannot send e-mail with ID {EmailId}, because it doesn't exist.", command.EmailId);
            return;
        }

        if (!email.IsSent)
        {
            await emailProvider.SendEmailAsync(email.RecipientEmail, email.Subject, email.Body, email.TeamId);

            // Mark the e-mail as sent to prevent it from being sent again.
            email.IsSent = true;
        }
    }
}
