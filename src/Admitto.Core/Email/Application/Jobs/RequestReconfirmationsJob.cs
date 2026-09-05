using System.Globalization;
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
/// Evaluates every projected, active reconfirm policy on the fixed hourly Quartz
/// tick and on one-shot policy-close triggers. Reminder cadence is not persisted;
/// close triggers exist only to guarantee the terminal evaluation at non-hour
/// boundaries.
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
    public const string PolicyCloseTriggerGroup = "reconfirm-close";
    public const string PolicyCloseEventIdKey = "PolicyCloseEventId";
    public const string PolicyCloseAtKey = "PolicyCloseAt";

    public static TriggerKey PolicyCloseTriggerKey(
        TicketedEventId ticketedEventId,
        DateTimeOffset closesAt) =>
        new(
            $"{ticketedEventId.Value:N}.{closesAt.UtcTicks}",
            PolicyCloseTriggerGroup);

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var now = timeProvider.GetUtcNow();

        if (HasPolicyCloseTarget(context))
        {
            if (!TryGetPolicyCloseTarget(context, out var targetEventId, out var targetClosesAt))
            {
                logger.LogWarning("Ignoring malformed reconfirm policy-close trigger data.");
                return;
            }

            var targetedPolicy = await readStore.EventEmailContexts
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    c => c.TicketedEventId == targetEventId!.Value
                        && c.ReconfirmClosesAt == targetClosesAt,
                    ct);

            if (targetedPolicy is not null
                && targetedPolicy.HasCompleteReconfirmPolicy
                && now >= targetClosesAt)
            {
                await EvaluatePolicyAsync(targetedPolicy, now, ct, terminalOnly: true);
            }

            return;
        }

        var policies = (await readStore.EventEmailContexts
            .AsNoTracking()
            .Where(c => c.ReconfirmOpensAt <= now)
            .ToListAsync(ct))
            .Where(c => c.HasCompleteReconfirmPolicy)
            .ToList();

        foreach (var policy in policies)
        {
            ct.ThrowIfCancellationRequested();

            await EvaluatePolicyAsync(policy, now, ct, terminalOnly: false);
        }
    }

    private static bool HasPolicyCloseTarget(IJobExecutionContext context)
    {
        var jobData = context.MergedJobDataMap;
        return jobData is not null
            && (jobData.ContainsKey(PolicyCloseEventIdKey)
                || jobData.ContainsKey(PolicyCloseAtKey));
    }

    private async Task EvaluatePolicyAsync(
        EventEmailContextView policy,
        DateTimeOffset now,
        CancellationToken ct,
        bool terminalOnly)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var writeStore = scope.ServiceProvider.GetRequiredService<IEmailWriteStore>();
            var registrationsFacade = scope.ServiceProvider.GetRequiredService<IRegistrationsFacade>();
            var outbox = scope.ServiceProvider.GetRequiredKeyedService<IOutbox>(EmailModule.Key);
            var unitOfWork = scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(EmailModule.Key);

            if (terminalOnly || now >= policy.ReconfirmClosesAt!.Value)
            {
                await EvaluatePolicyCloseAsync(
                    policy,
                    writeStore,
                    registrationsFacade,
                    outbox,
                    unitOfWork,
                    now,
                    ct);
                return;
            }

            if (!TryGetTimeZone(policy.TimeZone!, out var timeZone))
                return;

            if (await HasOutstandingReconfirmJobAsync(writeStore, policy, ct))
                return;

            if (IsQuietHours(policy, now, timeZone))
                return;

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

    private static bool TryGetPolicyCloseTarget(
        IJobExecutionContext context,
        out TicketedEventId? eventId,
        out DateTimeOffset closesAt)
    {
        eventId = null;
        closesAt = default;
        var jobData = context.MergedJobDataMap;
        if (jobData is null)
            return false;

        var eventIdText = jobData.GetString(PolicyCloseEventIdKey);
        var closeText = jobData.GetString(PolicyCloseAtKey);
        if (!Guid.TryParse(eventIdText, out var eventGuid)
            || !DateTimeOffset.TryParse(
                closeText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out closesAt))
        {
            return false;
        }

        eventId = TicketedEventId.From(eventGuid);
        return true;
    }

    private async Task EvaluatePolicyCloseAsync(
        EventEmailContextView policy,
        IEmailWriteStore writeStore,
        IRegistrationsFacade registrationsFacade,
        IOutbox outbox,
        IUnitOfWork unitOfWork,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var closesAt = policy.ReconfirmClosesAt!.Value;
        var alreadyEvaluated = await writeStore.ReconfirmPolicyCloseEvaluations
            .AsNoTracking()
            .AnyAsync(e =>
                e.TeamId == policy.TeamId
                && e.TicketedEventId == policy.TicketedEventId
                && e.ClosesAt == closesAt,
                ct);
        if (alreadyEvaluated)
            return;

        var (candidates, sentReconfirmationLogs) = await LoadCandidatesAndLogsAsync(
            policy,
            writeStore,
            registrationsFacade,
            ct);

        var autoCancelCandidates = candidates
            .Where(r =>
                r.EffectiveMaxReconfirmationEmails.HasValue
                && GetCurrentCycleLogs(sentReconfirmationLogs, r).Count
                    >= r.EffectiveMaxReconfirmationEmails.Value)
            .ToList();

        if (autoCancelCandidates.Count > 0)
        {
            var registrationIds = autoCancelCandidates.Select(r => r.RegistrationId).ToList();
            var references = BuildAutoExpiredReferences(autoCancelCandidates);

            outbox.Enqueue(new ReconfirmAutoExpiredIntegrationEvent(
                policy.TeamId.Value,
                policy.TicketedEventId.Value,
                registrationIds,
                references));
        }

        writeStore.ReconfirmPolicyCloseEvaluations.Add(
            ReconfirmPolicyCloseEvaluation.Create(
                policy.TeamId,
                policy.TicketedEventId,
                closesAt,
                now));

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsPolicyCloseEvaluationReservationViolation(ex))
        {
            // Another evaluator recorded this policy close. Its marker and
            // cancellation event own the terminal evaluation.
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
        var (candidates, sentReconfirmationLogs) = await LoadCandidatesAndLogsAsync(
            policy,
            writeStore,
            registrationsFacade,
            ct);

        if (candidates.Count == 0)
            return;

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
            var references = BuildAutoExpiredReferences(
                eligibleCandidates.Where(r => autoCancelRegistrationIds.Contains(r.RegistrationId)));

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

    private static async Task<(
        IReadOnlyList<RegistrationListItemDto> Candidates,
        IReadOnlyList<ReconfirmLogData> SentReconfirmationLogs)> LoadCandidatesAndLogsAsync(
        EventEmailContextView policy,
        IEmailWriteStore writeStore,
        IRegistrationsFacade registrationsFacade,
        CancellationToken cancellationToken)
    {
        var candidates = await registrationsFacade.GetRegistrationsAsync(
            policy.TeamId.Value,
            policy.TicketedEventId.Value,
            new QueryRegistrationsDto(
                RegistrationStatus: RegistrationStatus.Registered,
                HasReconfirmed: false),
            cancellationToken);

        var sentReconfirmationLogs = await writeStore.EmailLog
            .AsNoTracking()
            .Where(l =>
                l.TeamId == policy.TeamId
                && l.TicketedEventId == policy.TicketedEventId
                && l.EmailType == BuiltInEmailTemplateNames.Reconfirmation
                && (l.Status == EmailLogStatus.Sent || l.Status == EmailLogStatus.Delivered)
                && l.SentAt.HasValue)
            .Select(l => new ReconfirmLogData(l.RegistrationCycleId, l.SentAt!.Value))
            .ToListAsync(cancellationToken);

        return (candidates, sentReconfirmationLogs);
    }

    private static IReadOnlyList<ReconfirmAutoExpiredRegistrationReference> BuildAutoExpiredReferences(
        IEnumerable<RegistrationListItemDto> registrations) =>
        registrations
            .Select(r => new ReconfirmAutoExpiredRegistrationReference(
                r.RegistrationId,
                r.RegistrationCycleId,
                r.RegistrationVersion,
                r.TicketCatalogVersion,
                r.TicketTypeIds))
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

    private static bool IsPolicyCloseEvaluationReservationViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgresException
        && postgresException.ConstraintName == "PK_reconfirm_policy_close_evaluations";
}
