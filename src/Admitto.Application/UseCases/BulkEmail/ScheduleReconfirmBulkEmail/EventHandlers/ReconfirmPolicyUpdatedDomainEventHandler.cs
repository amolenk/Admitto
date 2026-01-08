using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Application.UseCases.BulkEmail.ScheduleReconfirmBulkEmail.EventHandlers;

/// <summary>
/// When a reconfirm policy is updated, we need to reschedule the reconfirm bulk email.
/// </summary>
public class ReconfirmPolicyUpdatedDomainEventHandler(
    ScheduleReconfirmBulkEmailHandler scheduleReconfirmBulkEmailHandler)
    : IEventualDomainEventHandler<ReconfirmPolicyUpdatedDomainEvent>
{
    public async ValueTask HandleAsync(
        ReconfirmPolicyUpdatedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var command = new ScheduleReconfirmBulkEmailCommand(domainEvent.TeamId, domainEvent.TicketedEventId)
        {
            CommandId = DeterministicGuid.Create(
                $"{domainEvent.DomainEventId}:{nameof(ScheduleReconfirmBulkEmailCommand)}")
        };

        await scheduleReconfirmBulkEmailHandler.HandleAsync(command, cancellationToken);
    }
}