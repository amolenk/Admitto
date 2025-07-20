using Amolenk.Admitto.Application.Jobs.SendEmail;

namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail;

public class SendEmailHandler(IJobScheduler jobScheduler) : ICommandHandler<SendEmailCommand>
{
    public async ValueTask HandleAsync(SendEmailCommand command, CancellationToken cancellationToken)
    {
        var emailJobData = new SendEmailJobData(
            JobId: command.CommandId,
            command.TeamId,
            command.TicketedEventId,
            command.EmailType,
            command.DataEntityId,
            command.RecipientEmail);

        await jobScheduler.AddJobAsync(emailJobData, cancellationToken);
    }
}
