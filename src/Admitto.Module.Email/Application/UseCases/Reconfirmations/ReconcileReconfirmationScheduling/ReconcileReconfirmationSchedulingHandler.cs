using Amolenk.Admitto.Module.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.Reconfirmations.ReconcileReconfirmationScheduling;

/// <summary>
/// Walks the active reconfirm trigger specs from the Registrations module and
/// re-issues <see cref="ScheduleReconfirmationsCommand"/> for each, healing
/// any drift between Quartz state and the source of truth (e.g. after a
/// worker redeploy with a fresh Quartz store, or after missed events).
/// </summary>
[RequiresCapability(HostCapability.Jobs | HostCapability.Email)]
internal sealed class ReconcileReconfirmationSchedulingHandler(
    IRegistrationsFacade registrationsFacade,
    IMediator mediator,
    ILogger<ReconcileReconfirmationSchedulingHandler> logger)
    : ICommandHandler<ReconcileReconfirmationSchedulingCommand>
{
    public async ValueTask HandleAsync(
        ReconcileReconfirmationSchedulingCommand command,
        CancellationToken cancellationToken)
    {
        var specs = await registrationsFacade.GetActiveReconfirmTriggerSpecsAsync(cancellationToken);

        logger.LogInformation(
            "Reconciling {Count} reconfirm trigger(s).", specs.Count);

        foreach (var spec in specs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await mediator.SendAsync(
                new ScheduleReconfirmationsCommand(
                    TicketedEventId.From(spec.TicketedEventId),
                    spec),
                cancellationToken);
        }
    }
}
