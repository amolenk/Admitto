namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail;

/// <summary>
/// Sends an e-mail.
/// </summary>
public class SendEmailHandler(IEmailContext context, IEmailSender emailProvider)
    : ICommandHandler<SendEmailCommand>
{
    public async ValueTask HandleAsync(SendEmailCommand command, CancellationToken cancellationToken)
    {
        var email = await context.EmailMessages.FindAsync([command.EmailId], cancellationToken);
        if (email is null)
        {
            // TODO
            throw new Exception("Email not found.");
        }

        await emailProvider.SendEmailAsync(email.RecipientEmail, email.Subject, email.Body, email.TeamId,
            email.TicketedEventId, email.AttendeeId);
        
        // TODO After succesfully sending the e-mail, we should mark it as sent in the database.
    }
}