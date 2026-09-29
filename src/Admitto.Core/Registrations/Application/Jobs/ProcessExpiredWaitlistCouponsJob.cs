using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.ProcessWaitlistNotifications;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Quartz;

namespace Amolenk.Admitto.Core.Registrations.Application.Jobs;

/// <summary>
/// Polls for waitlists holding issued coupons whose offer lapsed (past the grace period) and processes
/// each one: expires the coupon on the <see cref="Waitlist"/> aggregate (which raises
/// <see cref="Domain.DomainEvents.WaitlistCouponExpiredDomainEvent"/> so the recipient is told
/// their offer lapsed), then fires
/// <see cref="ProcessWaitlistNotificationsCommand"/> to cascade the freed slots to the next
/// people in queue. Only <see cref="WaitlistCouponOrigin.Automatic"/> coupons free a slot: a
/// manually issued (VIP) coupon was never backed by one, so its expiry cascades nothing. The command
/// is fired even when no slot was freed, so WaitlistMode is still re-evaluated. If the waitlist is
/// empty after expiry, the domain raises
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

            // Same lapsed-coupon rule as Waitlist.GetLapsedCouponIds, expressed so it runs in the database.
            var waitlists = await writeStore.Waitlists
                .Where(w => w.Coupons.Any(c => c.Status == WaitlistCouponStatus.Issued && c.ExpiresAt <= cutoff))
                .ToListAsync(context.CancellationToken);

            if (waitlists.Count == 0)
                return;

            foreach (var waitlist in waitlists)
            {
                var catalog = await writeStore.TicketCatalogs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        c => c.Id == waitlist.EventId && c.TeamId == waitlist.TeamId,
                        context.CancellationToken);

                if (catalog is null || catalog.EventStatus != EventLifecycleStatus.Active)
                    continue;

                var lapsedCouponIds = waitlist.GetLapsedCouponIds(cutoff);

                logger.LogInformation(
                    "Expiring {Count} lapsed waitlist coupon(s) for ticket type {TicketTypeId}",
                    lapsedCouponIds.Count, waitlist.Id.Value);

                // The coupons are only needed for the recipient and code in the expired-offer email.
                var lapsedCoupons = await writeStore.Coupons
                    .AsNoTracking()
                    .Where(c => lapsedCouponIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id, context.CancellationToken);

                // Without a coupon or ticket type there is nothing to put in the expired-offer email; the
                // aggregate still expires the waitlist coupon so any freed slot cascades.
                var ticketType = catalog.GetTicketType(waitlist.Id);
                var freedSlots = 0;

                foreach (var couponId in lapsedCouponIds)
                {
                    if (waitlist.ExpireCoupon(couponId, lapsedCoupons.GetValueOrDefault(couponId), ticketType))
                        freedSlots++;
                }

                await notifyHandler.HandleAsync(
                    new ProcessWaitlistNotificationsCommand(
                        waitlist.EventId.Value, waitlist.TeamId.Value, waitlist.Id.Value, freedSlots),
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
