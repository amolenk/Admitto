using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.WriteActivityLog;

internal sealed class WriteActivityLogHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<WriteActivityLogCommand>
{
    public ValueTask HandleAsync(
        WriteActivityLogCommand command,
        CancellationToken cancellationToken)
    {
        writeStore.ActivityLog.Add(Domain.Entities.ActivityLog.Create(
            teamId: command.TeamId,
            eventId: command.EventId,
            registrationId: command.RegistrationId,
            activityType: command.ActivityType,
            occurredAt: command.OccurredAt,
            metadata: command.Metadata));

        return ValueTask.CompletedTask;
    }
}
