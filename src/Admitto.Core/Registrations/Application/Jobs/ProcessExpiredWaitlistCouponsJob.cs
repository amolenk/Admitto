using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
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
/// raises <see cref="Domain.DomainEvents.WaitlistExhaustedDomainEvent"/> which lifts WaitlistMode. After registration
/// has closed the freed seat goes to nobody, and the expired-offer email doesn't invite the attendee to register again.
/// </summary>
/// <remarks>
/// The 2-minute grace period (<see cref="GracePeriod"/>) prevents the job from racing with
/// a last-second redemption by the attendee. As a second line of defence, each event's waitlists and ticket
/// catalog carry a PostgreSQL <c>xmin</c> row-version concurrency token; if a redemption, registration or
/// cancellation on the same catalog commits first, that event's save fails with a
/// <see cref="DbUpdateConcurrencyException"/>, which is logged and left for the next run to retry. Each event is
/// processed and saved in its own unit of work (its own DI scope and <see cref="IRegistrationsWriteStore"/>), so a
/// conflict on one event's catalog doesn't roll back or block any other event's work in the same run.
/// </remarks>
[DisallowConcurrentExecution]
internal sealed class ProcessExpiredWaitlistCouponsJob(
    IRegistrationsWriteStore writeStore,
    IServiceScopeFactory scopeFactory,
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

            // Only active events have a waitlist worth expiring coupons for; events that archived in the
            // meantime are excluded here instead of being fetched and skipped on every run.
            var activeEventIds = writeStore.TicketCatalogs
                .Where(c => c.EventStatus == EventLifecycleStatus.Active)
                .Select(c => c.Id);

            // Same lapsed-coupon rule as Waitlist.GetLapsedCouponIds, expressed so it runs in the database.
            var eventKeys = await writeStore.Waitlists
                .Where(w => activeEventIds.Contains(w.EventId)
                    && w.Coupons.Any(c => c.Status == WaitlistCouponStatus.Issued && c.ExpiresAt <= cutoff))
                .Select(w => new { w.EventId, w.TeamId })
                .Distinct()
                .ToListAsync(context.CancellationToken);

            foreach (var key in eventKeys)
            {
                await ProcessEventAsync(key.EventId, key.TeamId, now, cutoff, context.CancellationToken);
            }
        }
        catch (Exception e)
        {
            throw new JobExecutionException(e);
        }
    }

    /// <summary>
    /// Expires every lapsed coupon for one event's waitlists and saves the result in its own unit of work. A
    /// concurrency conflict here is logged and left for the next run; it never reaches <see cref="Execute"/>, so
    /// it can't roll back or interrupt any other event's work in the same run.
    /// </summary>
    private async Task ProcessEventAsync(
        TicketedEventId eventId,
        TeamId teamId,
        DateTimeOffset now,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var eventWriteStore = scope.ServiceProvider.GetRequiredService<IRegistrationsWriteStore>();
        var unitOfWork = scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(RegistrationsModule.Key);

        try
        {
            var catalog = await eventWriteStore.TicketCatalogs
                .FirstOrDefaultAsync(c => c.Id == eventId && c.TeamId == teamId, cancellationToken);

            if (catalog is null || catalog.EventStatus != EventLifecycleStatus.Active)
                return;

            var waitlists = await eventWriteStore.Waitlists
                .Where(w => w.EventId == eventId && w.TeamId == teamId
                    && w.Coupons.Any(c => c.Status == WaitlistCouponStatus.Issued && c.ExpiresAt <= cutoff))
                .ToListAsync(cancellationToken);

            if (waitlists.Count == 0)
                return;

            var ticketedEvent = await eventWriteStore.TicketedEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId && e.TeamId == teamId, cancellationToken);
            var registrationClosed = ticketedEvent?.HasRegistrationClosed(now) ?? false;

            foreach (var waitlist in waitlists)
            {
                var lapsedCouponIds = waitlist.GetLapsedCouponIds(cutoff);

                logger.LogInformation(
                    "Expiring {Count} lapsed waitlist coupon(s) for ticket type {TicketTypeId}",
                    lapsedCouponIds.Count, waitlist.Id.Value);

                // The coupons are only needed for the recipient and code in the expired-offer email.
                var lapsedCoupons = await eventWriteStore.Coupons
                    .AsNoTracking()
                    .Where(c => lapsedCouponIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id, cancellationToken);

                // Without a coupon or ticket type there is nothing to put in the expired-offer email; the
                // aggregate still expires the waitlist coupon and gives back its hold.
                foreach (var couponId in lapsedCouponIds)
                {
                    waitlist.ExpireCoupon(
                        couponId, lapsedCoupons.GetValueOrDefault(couponId), catalog, registrationClosed);
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken, retryConcurrencyConflicts: true);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(
                ex,
                "Concurrency conflict expiring waitlist coupons for event {EventId}; the next run will retry it.",
                eventId.Value);
        }
    }
}
