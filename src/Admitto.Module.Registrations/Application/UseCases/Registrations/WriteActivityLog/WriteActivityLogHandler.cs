using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.WriteActivityLog;

internal sealed class WriteActivityLogHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<WriteActivityLogCommand>
{
    public ValueTask HandleAsync(
        WriteActivityLogCommand command,
        CancellationToken cancellationToken)
    {
        writeStore.ActivityLog.Add(Domain.Entities.ActivityLog.Create(
            registrationId: command.RegistrationId.Value,
            activityType: command.ActivityType,
            occurredAt: command.OccurredAt,
            metadata: command.Metadata));

        return ValueTask.CompletedTask;
    }
}
