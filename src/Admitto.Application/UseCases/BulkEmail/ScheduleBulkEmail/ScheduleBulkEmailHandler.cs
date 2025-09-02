using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.BulkEmail.ScheduleBulkEmail;

public class ScheduleBulkEmailHandler(IApplicationContext context) : ICommandHandler<ScheduleBulkEmailCommand>
{
    public ValueTask HandleAsync(ScheduleBulkEmailCommand command, CancellationToken cancellationToken)
    {
        var bulkEmailJob = BulkEmailWorkItem.Create(
            command.TeamId,
            command.TicketedEventId,
            command.EmailType,
            command.Repeat);

        context.BulkEmailWorkItems.Add(bulkEmailJob);

        return ValueTask.CompletedTask;
    }
}