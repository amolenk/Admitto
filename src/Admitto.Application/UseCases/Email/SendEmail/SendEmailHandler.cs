using Amolenk.Admitto.Application.Jobs.SendEmail;

namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail;

// TODO Simple one-off emails don't need jobs

public class SendEmailHandler(IJobScheduler jobScheduler) : ICommandHandler<SendEmailCommand>
{
    public async ValueTask HandleAsync(SendEmailCommand command, CancellationToken cancellationToken)
    {
        var emailJobData = new SendEmailJobData(
            JobId: command.CommandId,
            command.TeamId,
            command.TicketedEventId,
            command.DataEntityId,
            command.EmailType,
            command.RecipientEmail);

        await jobScheduler.AddJobAsync(emailJobData, cancellationToken);
    }
}
