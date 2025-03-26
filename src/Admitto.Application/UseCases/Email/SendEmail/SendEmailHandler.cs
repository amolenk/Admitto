namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail;

/// <summary>
/// Sends an e-mail.
/// </summary>
public class SendEmailHandler(IEmailContext context, IEmailProvider emailProvider)
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

        await emailProvider.SendEmailAsync(email.TicketedEventId ?? Guid.Empty, email.RecipientEmail, "Magic",
            "body");
    }
}