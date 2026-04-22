using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Amolenk.Admitto.Module.Organization.Application.Jobs;

/// <summary>
/// Scheduled maintenance job that marks in-flight <c>TeamEventCreationRequest</c>s as
/// <c>Expired</c> once they have been pending longer than
/// <see cref="PendingTimeout"/>. Expiring a request decrements the owning team's
/// <c>PendingEventCount</c>, unblocking archive flows for stalled requests.
/// </summary>
/// <remarks>
/// Idempotent via <see cref="Team.ExpireEventCreationRequest"/>; the job is safe to
/// re-run after a partial failure. Runs out-of-band of any Registrations delivery, so
/// a late <c>TicketedEventCreated</c> or <c>TicketedEventCreationRejected</c> on an
/// already-expired request becomes a no-op — matching the aggregate's idempotency.
/// </remarks>
[RequiresCapability(HostCapability.Jobs)]
[DisallowConcurrentExecution]
public sealed class ExpireStaleEventCreationRequestsJob(
    IOrganizationWriteStore writeStore,
    [FromKeyedServices(OrganizationModuleKey.Value)] IUnitOfWork unitOfWork,
    ILogger<ExpireStaleEventCreationRequestsJob> logger)
    : IJob
{
    public const string Name = nameof(ExpireStaleEventCreationRequestsJob);

    /// <summary>
    /// How long a creation request may remain in <c>Pending</c> before the maintenance
    /// job expires it. Tracks the 24h SLA quoted in the change proposal.
    /// </summary>
    public static readonly TimeSpan PendingTimeout = TimeSpan.FromHours(24);

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var cutoff = now - PendingTimeout;

            // Narrow the team fetch to those that actually have stale pending requests.
            var teams = await writeStore.Teams
                .Where(t => t.EventCreationRequests.Any(r =>
                    r.Status == TeamEventCreationRequestStatus.Pending && r.RequestedAt <= cutoff))
                .ToListAsync(context.CancellationToken);

            if (teams.Count == 0) return;

            foreach (var team in teams)
            {
                var stale = team.EventCreationRequests
                    .Where(r => r.Status == TeamEventCreationRequestStatus.Pending && r.RequestedAt <= cutoff)
                    .Select(r => r.Id)
                    .ToList();

                foreach (var requestId in stale)
                {
                    logger.LogInformation(
                        "Expiring stale event creation request {CreationRequestId} on team {TeamId}",
                        requestId.Value, team.Id.Value);

                    team.ExpireEventCreationRequest(requestId, now);
                }
            }

            await unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
        catch (Exception e)
        {
            throw new JobExecutionException(e);
        }
    }
}
