using Amolenk.Admitto.Module.Email.Application.ModuleEvents;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.TriggerBulkEmailJob.EventHandlers;

/// <summary>
/// Handles <see cref="BulkEmailJobRequestedModuleEvent"/> by dispatching a
/// <see cref="TriggerBulkEmailJobCommand"/>.
/// </summary>
/// <remarks>
/// No capability gate — this handler runs in any host that processes the Email queue.
/// The actual scheduling is gated on <see cref="HostCapability.Jobs"/> inside
/// <see cref="TriggerBulkEmailJobHandler"/>.
/// </remarks>
internal sealed class BulkEmailJobRequestedModuleEventHandler(IMediator mediator)
    : IModuleEventHandler<BulkEmailJobRequestedModuleEvent>
{
    public ValueTask HandleAsync(
        BulkEmailJobRequestedModuleEvent moduleEvent,
        CancellationToken cancellationToken)
    {
        var command = new TriggerBulkEmailJobCommand(
            BulkEmailJobId.From(moduleEvent.BulkEmailJobId));

        return mediator.SendAsync(command, cancellationToken);
    }
}
