using Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetActiveReconfirmTriggerSpecs;
using Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ReconcileReconfirmationScheduling;

/// <summary>
/// Walks the active reconfirm trigger specs from the Email-owned event context
/// projection and re-issues <see cref="ScheduleReconfirmationsCommand"/> for
/// each, healing any drift between Quartz state and the projection (e.g. after a
/// worker redeploy with a fresh Quartz store, or after missed events).
/// </summary>
internal sealed class ReconcileReconfirmationSchedulingHandler(
    IQueryHandler<GetActiveReconfirmTriggerSpecsQuery, IReadOnlyList<ReconfirmTriggerSpecDto>> activeSpecsQuery,
    ICommandHandler<ScheduleReconfirmationsCommand> scheduleReconfirmationsHandler,
    ILogger<ReconcileReconfirmationSchedulingHandler> logger)
    : ICommandHandler<ReconcileReconfirmationSchedulingCommand>, IWorkerOnly
{
    public async ValueTask HandleAsync(
        ReconcileReconfirmationSchedulingCommand command,
        CancellationToken cancellationToken)
    {
        var specs = await activeSpecsQuery.HandleAsync(
            new GetActiveReconfirmTriggerSpecsQuery(),
            cancellationToken);

        logger.LogInformation(
            "Reconciling {Count} reconfirm trigger(s).", specs.Count);

        foreach (var spec in specs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await scheduleReconfirmationsHandler.HandleAsync(
                new ScheduleReconfirmationsCommand(
                    spec.TicketedEventId,
                    spec),
                cancellationToken);
        }
    }
}
