using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Quartz;

namespace Amolenk.Admitto.Core.Registrations.Application.Jobs;

/// <summary>
/// Polls for waitlists holding issued coupons whose offer lapsed (past the grace period) and expires
/// each one on the <see cref="Waitlist"/> aggregate, which raises
/// <see cref="Domain.DomainEvents.WaitlistCouponExpiredDomainEvent"/> so the recipient is told
/// their offer lapsed, and gives back the public seat an automatic offer held on the <see cref="TicketCatalog"/>
/// (a VIP offer held none). The job counts no freed slots: the catalog decides from real capacity whether that
/// seat goes to the next person in queue (<see cref="Domain.DomainEvents.WaitlistCapacityAvailableDomainEvent"/>)
/// or makes up a shortfall left by lowering <c>PublicCapacity</c>. If the waitlist is empty after expiry, the domain
/// raises <see cref="Domain.DomainEvents.WaitlistExhaustedDomainEvent"/> which lifts WaitlistMode.
/// </summary>
/// <remarks>
/// The 2-minute grace period (<see cref="GracePeriod"/>) prevents the job from racing with
/// a last-second redemption by the attendee. As a second line of defence, the job writes both the
/// <see cref="Waitlist"/> and the <see cref="TicketCatalog"/>, which each carry a PostgreSQL
/// <c>xmin</c> row-version concurrency token; if a redemption, registration or cancellation on the
/// same catalog commits first, the job's save fails with a
/// <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/> and the next run retries.
/// </remarks>
[DisallowConcurrentExecution]
internal sealed class ProcessExpiredWaitlistCouponsJob(
    IRegistrationsWriteStore writeStore,
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
                // aggregate still expires the waitlist coupon and gives back its hold.
                foreach (var couponId in lapsedCouponIds)
                {
                    waitlist.ExpireCoupon(couponId, lapsedCoupons.GetValueOrDefault(couponId), catalog);
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
