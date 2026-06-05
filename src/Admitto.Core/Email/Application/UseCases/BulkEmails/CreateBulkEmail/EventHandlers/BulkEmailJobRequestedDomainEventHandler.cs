using Amolenk.Admitto.Core.Email.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CreateBulkEmail.EventHandlers;

/// <summary>
/// Enqueues a <see cref="TriggerBulkEmailJob.TriggerBulkEmailJobCommand"/> via the outbox
/// so the Worker host can schedule the Quartz fan-out outside the current transaction.
/// </summary>
internal sealed class BulkEmailJobRequestedDomainEventHandler(
    [FromKeyedServices(EmailModule.Key)] IOutbox outbox)
    : IDomainEventHandler<BulkEmailJobRequestedDomainEvent>
{
    public ValueTask HandleAsync(
        BulkEmailJobRequestedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        outbox.Enqueue(new TriggerBulkEmailJob.TriggerBulkEmailJobCommand(domainEvent.BulkEmailJobId.Value));

        return ValueTask.CompletedTask;
    }
}
