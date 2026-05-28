using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.ProcessWaitlistNotifications;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Amolenk.Admitto.Core.Registrations.Application.Jobs;

/// <summary>
/// Polls for expired waitlist coupons (past the grace period) and processes each one:
/// revokes the coupon on the <see cref="Waitlist"/> aggregate, then fires
/// <see cref="ProcessWaitlistNotificationsCommand"/> to cascade the freed slot to the next
/// person in queue. If the waitlist is empty after revocation, the domain raises
/// <see cref="Domain.DomainEvents.WaitlistExhaustedDomainEvent"/> which lifts WaitlistMode.
/// </summary>
/// <remarks>
/// The 2-minute grace period (<see cref="GracePeriod"/>) prevents the job from racing with
/// a last-second redemption by the attendee. As a second line of defence, the
/// <see cref="Waitlist"/> aggregate carries a PostgreSQL <c>xmin</c> row-version concurrency
/// token; if both transactions attempt to commit simultaneously the loser receives a
/// <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/> which surfaces as
/// a <see cref="Shared.Kernel.ErrorHandling.ConcurrencyConflictError"/> at the API layer.
/// </remarks>
[DisallowConcurrentExecution]
internal sealed class ProcessExpiredWaitlistCouponsJob(
    IRegistrationsWriteStore writeStore,
    ICommandHandler<ProcessWaitlistNotificationsCommand> notifyHandler,
    [FromKeyedServices(RegistrationsModule.Key)] IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ProcessExpiredWaitlistCouponsJob> logger)
    : IJob
{
    public const string Name = nameof(ProcessExpiredWaitlistCouponsJob);

    /// <summary>
    /// Grace period subtracted from the current time before considering a coupon expired.
    /// Prevents racing with last-second redemptions.
    /// </summary>
    public static readonly TimeSpan GracePeriod = TimeSpan.FromMinutes(2);

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var now = timeProvider.GetUtcNow();
            var cutoff = now - GracePeriod;

            var expiredCoupons = await writeStore.Coupons
                .Where(c =>
                    c.Source == CouponSource.Waitlist &&
                    c.RedeemedAt == null &&
                    c.RevokedAt == null &&
                    c.ExpiresAt <= cutoff)
                .ToListAsync(context.CancellationToken);

            if (expiredCoupons.Count == 0)
                return;

            // Waitlist coupons always target exactly one ticket type — group to batch per type.
            var groups = expiredCoupons
                .GroupBy(c => (EventId: c.EventId, TicketTypeId: c.AllowedTicketTypeIds[0]));

            foreach (var group in groups)
            {
                var (eventId, ticketTypeId) = group.Key;
                var couponsToRevoke = group.ToList();

                logger.LogInformation(
                    "Revoking {Count} expired waitlist coupon(s) for ticket type {TicketTypeId}",
                    couponsToRevoke.Count, ticketTypeId.Value);

                var waitlist = await writeStore.Waitlists
                    .Include(w => w.Entries)
                    .Include(w => w.Coupons)
                    .FirstOrDefaultAsync(w => w.Id == ticketTypeId, context.CancellationToken);

                if (waitlist is null)
                {
                    logger.LogWarning(
                        "Waitlist not found for ticket type {TicketTypeId} — skipping revocation",
                        ticketTypeId.Value);
                    continue;
                }

                foreach (var coupon in couponsToRevoke)
                {
                    waitlist.RevokeCoupon(coupon.Id);
                    coupon.Revoke();
                }

                await notifyHandler.HandleAsync(
                    new ProcessWaitlistNotificationsCommand(
                        eventId.Value, ticketTypeId.Value, couponsToRevoke.Count),
                    context.CancellationToken);
            }

            await unitOfWork.SaveChangesAsync(context.CancellationToken);
        }
        catch (Exception e)
        {
            throw new JobExecutionException(e);
        }
    }
}
