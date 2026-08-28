using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quartz;

namespace Amolenk.Admitto.Core.Email.Application.Jobs;

/// <summary>
/// Evaluates every projected, active reconfirm policy once per hourly Quartz
/// tick. The operational trigger is stable and hourly; policy cadence is not
/// persisted because attendee throttling is the only per-recipient schedule.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class RequestReconfirmationsJob(
    IEmailReadStore readStore,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<RequestReconfirmationsJob> logger)
    : IJob
{
    public const string Name = nameof(RequestReconfirmationsJob);
    public const string TriggerName = $"{Name}.Hourly";

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var now = timeProvider.GetUtcNow();

        var policies = (await readStore.EventEmailContexts
            .AsNoTracking()
            .Where(c => c.ReconfirmOpensAt <= now && now < c.ReconfirmClosesAt)
            .ToListAsync(ct))
            .Where(c => c.HasCompleteReconfirmPolicy)
            .ToList();

        foreach (var policy in policies)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                if (!TryGetTimeZone(policy.TimeZone!, out var timeZone))
                    continue;

                await using var scope = scopeFactory.CreateAsyncScope();
                var writeStore = scope.ServiceProvider.GetRequiredService<IEmailWriteStore>();
                var registrationsFacade = scope.ServiceProvider.GetRequiredService<IRegistrationsFacade>();
                var outbox = scope.ServiceProvider.GetRequiredKeyedService<IOutbox>(EmailModule.Key);
                var unitOfWork = scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(EmailModule.Key);

                if (await HasOutstandingReconfirmJobAsync(writeStore, policy, ct))
                    continue;

                if (IsQuietHours(policy, now, timeZone))
                    continue;

                await EvaluateEventAsync(
                    policy,
                    writeStore,
                    registrationsFacade,
                    outbox,
                    unitOfWork,
                    now,
                    ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // An event is the unit of work. A bad projection or a failed
                // event should not prevent unrelated events being evaluated.
                logger.LogError(ex,
                    "Reconfirm evaluation failed for event {TicketedEventId}.",
                    policy.TicketedEventId.Value);
            }
        }
    }

    private async Task EvaluateEventAsync(
        EventEmailContextView policy,
        IEmailWriteStore writeStore,
        IRegistrationsFacade registrationsFacade,
        IOutbox outbox,
        IUnitOfWork unitOfWork,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var candidates = await registrationsFacade.GetRegistrationsAsync(
            policy.TeamId.Value,
            policy.TicketedEventId.Value,
            new QueryRegistrationsDto(
                RegistrationStatus: RegistrationStatus.Registered,
                HasReconfirmed: false),
            ct);

        if (candidates.Count == 0)
            return;

        var sentReconfirmationLogs = await writeStore.EmailLog
            .AsNoTracking()
            .Where(l =>
                l.TeamId == policy.TeamId
                && l.TicketedEventId == policy.TicketedEventId
                && l.EmailType == BuiltInEmailTemplateNames.Reconfirmation
                && (l.Status == EmailLogStatus.Sent || l.Status == EmailLogStatus.Delivered)
                && l.SentAt.HasValue)
            .Select(l => new ReconfirmLogData(l.RegistrationCycleId, l.SentAt!.Value))
            .ToListAsync(ct);

        var interval = TimeSpan.FromHours(policy.ReconfirmMinEmailIntervalHours!.Value);
        var eligibleCandidates = candidates
            .Where(r =>
            {
                var lastSentAt = GetCurrentCycleLogs(sentReconfirmationLogs, r).MaxBy(l => l.SentAt)?.SentAt;
                var baseline = lastSentAt.HasValue && lastSentAt.Value > r.CreatedAt
                    ? lastSentAt.Value
                    : r.CreatedAt;
                return baseline + interval <= now;
            })
            .ToList();

        if (eligibleCandidates.Count == 0)
            return;

        var reconfirmRegistrationIds = eligibleCandidates
            .Where(r =>
            {
                if (r.EffectiveMaxReconfirmationEmails is null)
                    return true;

                return GetCurrentCycleLogs(sentReconfirmationLogs, r).Count
                    < r.EffectiveMaxReconfirmationEmails.Value;
            })
            .Select(r => r.RegistrationId)
            .ToList();

        var autoCancelRegistrationIds = eligibleCandidates
            .Where(r =>
            {
                if (r.EffectiveMaxReconfirmationEmails is null)
                    return false;

                return GetCurrentCycleLogs(sentReconfirmationLogs, r).Count
                    >= r.EffectiveMaxReconfirmationEmails.Value;
            })
            .Select(r => r.RegistrationId)
            .ToList();

        if (reconfirmRegistrationIds.Count > 0)
        {
            var filter = new BulkEmailAttendeeFilter(
                RegistrationStatus: RegistrationStatus.Registered,
                HasReconfirmed: false,
                RegistrationIds: reconfirmRegistrationIds,
                RegistrationCycleIds: eligibleCandidates
                    .Where(r => reconfirmRegistrationIds.Contains(r.RegistrationId))
                    .ToDictionary(r => r.RegistrationId, r => r.RegistrationCycleId));

            writeStore.BulkEmailJobs.Add(BulkEmailJob.CreateSystemTriggered(
                policy.TeamId,
                policy.TicketedEventId,
                BuiltInEmailTemplateNames.Reconfirmation,
                subject: null,
                textBody: null,
                htmlBody: null,
                attendeeFilter: filter,
                now: now));
        }

        if (autoCancelRegistrationIds.Count > 0)
        {
            var references = eligibleCandidates
                .Where(r => autoCancelRegistrationIds.Contains(r.RegistrationId))
                .Select(r => new ReconfirmAutoExpiredRegistrationReference(
                    r.RegistrationId,
                    r.RegistrationCycleId,
                    r.RegistrationVersion,
                    r.TicketCatalogVersion,
                    r.TicketTypeIds))
                .ToList();

            outbox.Enqueue(new ReconfirmAutoExpiredIntegrationEvent(
                policy.TeamId.Value,
                policy.TicketedEventId.Value,
                autoCancelRegistrationIds,
                references));
        }

        if (reconfirmRegistrationIds.Count > 0 || autoCancelRegistrationIds.Count > 0)
        {
            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsActiveReconfirmReservationViolation(ex))
            {
                // Another evaluator won the durable reservation. Its job owns
                // this event's pending work, so this evaluation is complete.
            }
        }
    }

    private static async Task<bool> HasOutstandingReconfirmJobAsync(
        IEmailWriteStore writeStore,
        EventEmailContextView policy,
        CancellationToken cancellationToken) =>
        await writeStore.BulkEmailJobs
            .AsNoTracking()
            .AnyAsync(j =>
                j.TeamId == policy.TeamId
                && j.TicketedEventId == policy.TicketedEventId
                && j.EmailType == BuiltInEmailTemplateNames.Reconfirmation
                && j.IsSystemTriggered
                && (j.Status == BulkEmailJobStatus.Pending
                    || j.Status == BulkEmailJobStatus.Resolving
                    || j.Status == BulkEmailJobStatus.Sending),
                cancellationToken);

    private static IReadOnlyList<ReconfirmLogData> GetCurrentCycleLogs(
        IReadOnlyList<ReconfirmLogData> logs,
        RegistrationListItemDto registration) =>
        logs.Where(log =>
                log.SentAt >= registration.CreatedAt
                && log.RegistrationCycleId == RegistrationCycleId.From(registration.RegistrationCycleId))
            .ToList();

    private sealed record ReconfirmLogData(
        RegistrationCycleId? RegistrationCycleId,
        DateTimeOffset SentAt);

    private static bool TryGetTimeZone(string timeZoneId, out TimeZoneInfo timeZone)
    {
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = default!;
        }
        catch (InvalidTimeZoneException)
        {
            timeZone = default!;
        }

        return false;
    }

    private static bool IsQuietHours(
        EventEmailContextView policy,
        DateTimeOffset now,
        TimeZoneInfo timeZone)
    {
        if (!policy.ReconfirmQuietHoursStart.HasValue || !policy.ReconfirmQuietHoursEnd.HasValue)
            return false;

        var localTime = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);
        var start = policy.ReconfirmQuietHoursStart.Value;
        var end = policy.ReconfirmQuietHoursEnd.Value;

        // Quiet hours are [start, end), with start > end denoting an overnight
        // interval. Equal times are rejected by the domain policy.
        return start < end
            ? localTime >= start && localTime < end
            : localTime >= start || localTime < end;
    }

    private static bool IsActiveReconfirmReservationViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgresException
        && postgresException.ConstraintName == "IX_bulk_email_jobs_active_reconfirm_event";
}
