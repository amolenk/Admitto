using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Projections.EventEmailContext;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Quartz;

namespace Amolenk.Admitto.Core.Email.Application.Jobs;

/// <summary>
/// Evaluates every projected reconfirmation policy on the one fixed hourly
/// trigger and sends live candidates directly. EmailLog is the durable claim,
/// delivery status, audit record, and idempotency boundary for each attempt.
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
        await FailOrphanedPendingReconfirmationsAsync(ct);

        var now = timeProvider.GetUtcNow();
        await using var smtp = new RunSmtpSession(scopeFactory);

        var policies = (await readStore.EventEmailContexts
                .AsNoTracking()
                .Where(c => c.ReconfirmOpensAt <= now)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync(ct))
            .Where(c => c.HasCompleteReconfirmPolicy)
            .ToList();

        foreach (var policy in policies)
        {
            ct.ThrowIfCancellationRequested();
            var policyNow = timeProvider.GetUtcNow();
            await EvaluatePolicyAsync(policy, policyNow, smtp, ct);
        }
    }

    private async Task FailOrphanedPendingReconfirmationsAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var writeStore = scope.ServiceProvider.GetRequiredService<IEmailWriteStore>();
        var unitOfWork = scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(EmailModule.Key);
        var pending = await writeStore.EmailLog
            .Where(log => log.EmailType == BuiltInEmailTemplateNames.Reconfirmation
                && log.Status == EmailLogStatus.Pending)
            .ToListAsync(ct);

        if (pending.Count == 0)
            return;

        var now = timeProvider.GetUtcNow();
        foreach (var log in pending)
            log.MarkFailed(log.Subject, "Reconfirmation delivery was interrupted before completion.", now);

        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task EvaluatePolicyAsync(
        EventEmailContextView policy,
        DateTimeOffset now,
        RunSmtpSession smtp,
        CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var writeStore = scope.ServiceProvider.GetRequiredService<IEmailWriteStore>();
            var registrationsFacade = scope.ServiceProvider.GetRequiredService<IRegistrationsFacade>();
            var outbox = scope.ServiceProvider.GetRequiredKeyedService<IOutbox>(EmailModule.Key);
            var unitOfWork = scope.ServiceProvider.GetRequiredKeyedService<IUnitOfWork>(EmailModule.Key);
            var teamId = policy.TeamId;
            var eventId = policy.TicketedEventId;

            if (now >= policy.ReconfirmClosesAt!.Value)
            {
                await EvaluatePolicyCloseAsync(
                    policy, writeStore, registrationsFacade, outbox, unitOfWork, now, ct);
                return;
            }

            if (!TryGetTimeZone(policy.TimeZone!, out var timeZone)
                || IsQuietHours(policy, now, timeZone))
                return;

            var (candidates, sentReconfirmationLogs) = await LoadCandidatesAndLogsAsync(
                policy, writeStore, registrationsFacade, ct);

            var eligibleCandidates = candidates
                .Where(registration =>
                {
                    var currentLogs = GetCurrentCycleLogs(sentReconfirmationLogs, registration);
                    var lastSentAt = currentLogs.MaxBy(log => log.SentAt)?.SentAt;
                    var baseline = lastSentAt.HasValue && lastSentAt.Value > registration.CreatedAt
                        ? lastSentAt.Value
                        : registration.CreatedAt;
                    return baseline + TimeSpan.FromHours(policy.ReconfirmMinEmailIntervalHours!.Value) <= now;
                })
                .ToList();

            var reconfirmCandidates = eligibleCandidates
                .Where(registration =>
                    registration.EffectiveMaxReconfirmationEmails is null
                    || GetCurrentCycleLogs(sentReconfirmationLogs, registration).Count
                        < registration.EffectiveMaxReconfirmationEmails.Value)
                .ToList();

            var autoCancelCandidates = eligibleCandidates
                .Where(registration =>
                    registration.EffectiveMaxReconfirmationEmails.HasValue
                    && GetCurrentCycleLogs(sentReconfirmationLogs, registration).Count
                        >= registration.EffectiveMaxReconfirmationEmails.Value)
                .ToList();

            // Candidate selection can take long enough to cross a policy gate.
            // The delivery start instant is retained for the admission fallback,
            // allowing an already-started hourly run to finish its candidates.
            var deliveryStart = timeProvider.GetUtcNow();
            if (deliveryStart >= policy.ReconfirmClosesAt!.Value)
            {
                await EvaluatePolicyCloseAsync(
                    policy, writeStore, registrationsFacade, outbox, unitOfWork, deliveryStart, ct);
                return;
            }

            if (IsQuietHours(policy, deliveryStart, timeZone))
                return;

            if (autoCancelCandidates.Count > 0)
            {
                outbox.Enqueue(new ReconfirmAutoExpiredIntegrationEvent(
                    teamId.Value,
                    eventId.Value,
                    autoCancelCandidates.Select(r => r.RegistrationId).ToList(),
                    BuildAutoExpiredReferences(autoCancelCandidates)));

                // Commit the cancellation request before preparing email delivery.
                // SMTP/settings/template failures must not roll back this durable
                // cross-module work.
                await unitOfWork.SaveChangesAsync(ct);
            }

            if (reconfirmCandidates.Count == 0)
                return;

            await DeliverCandidatesAsync(
                teamId,
                eventId,
                deliveryStart,
                reconfirmCandidates,
                writeStore,
                registrationsFacade,
                scope.ServiceProvider.GetRequiredService<ITransactionalEmailComposer>(),
                smtp,
                scope.ServiceProvider.GetRequiredService<IOptionsMonitor<EmailDeliveryOptions>>(),
                timeProvider,
                unitOfWork,
                ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            logger.LogWarning(
                "Reconfirm evaluation cancelled for event {TicketedEventId}.",
                policy.TicketedEventId.Value);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Reconfirm evaluation failed for event {TicketedEventId}.",
                policy.TicketedEventId.Value);
        }
    }

    private static async Task DeliverCandidatesAsync(
        TeamId teamId,
        TicketedEventId eventId,
        DateTimeOffset deliveryStart,
        IReadOnlyList<RegistrationListItemDto> candidates,
        IEmailWriteStore writeStore,
        IRegistrationsFacade registrationsFacade,
        ITransactionalEmailComposer composition,
        RunSmtpSession smtp,
        IOptionsMonitor<EmailDeliveryOptions> options,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var compositionScope = await composition.CreateReconfirmationScopeAsync(
            teamId,
            eventId,
            ct);

        foreach (var candidate in candidates)
        {
            ct.ThrowIfCancellationRequested();
            await DeliverCandidateAsync(
                teamId,
                eventId,
                deliveryStart,
                candidate,
                compositionScope,
                registrationsFacade,
                writeStore,
                smtp,
                options,
                timeProvider,
                unitOfWork,
                ct);
        }
    }

    private static async Task DeliverCandidateAsync(
        TeamId teamId,
        TicketedEventId eventId,
        DateTimeOffset deliveryStart,
        RegistrationListItemDto candidate,
        ReconfirmationEmailCompositionScope compositionScope,
        IRegistrationsFacade registrationsFacade,
        IEmailWriteStore writeStore,
        RunSmtpSession smtp,
        IOptionsMonitor<EmailDeliveryOptions> options,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var deliveryState = await GetCurrentAdmissionAsync(
            teamId,
            eventId,
            candidate,
            registrationsFacade,
            writeStore,
            deliveryStart,
            timeProvider,
            ct);
        if (deliveryState is null)
            return;

        var registrationId = RegistrationId.From(candidate.RegistrationId);
        var registrationCycleId = RegistrationCycleId.From(candidate.RegistrationCycleId);
        var intent = new ReconfirmationIntent(candidate.FirstName, registrationId);
        var rendered = compositionScope.Render(intent);
        var session = await smtp.GetOrOpenAsync(ct);
        var message = new EmailMessage(
            candidate.Email,
            string.Concat(candidate.FirstName, " ", candidate.LastName).Trim(),
            rendered.Subject,
            rendered.TextBody,
            rendered.HtmlBody);

        var idempotencyKey =
            $"reconfirm:{candidate.RegistrationId:N}:{candidate.RegistrationCycleId:N}:{Guid.NewGuid():N}";
        var log = EmailLog.Create(
            teamId,
            eventId,
            idempotencyKey,
            EmailAddress.From(candidate.Email),
            BuiltInEmailTemplateNames.Reconfirmation,
            rendered.Subject,
            EmailLogStatus.Pending,
            null,
            now,
            registrationId: registrationId,
            registrationCycleId: registrationCycleId);
        writeStore.EmailLog.Add(log);
        await unitOfWork.SaveChangesAsync(ct);

        try
        {
            var delivered = await SendWithInlineRetriesAsync(
                session,
                message,
                options,
                async admissionToken => (await GetCurrentAdmissionAsync(
                    teamId,
                    eventId,
                    candidate,
                    registrationsFacade,
                    writeStore,
                    deliveryStart,
                    timeProvider,
                    admissionToken)) is not null,
                ct);
            if (!delivered)
            {
                writeStore.EmailLog.Remove(log);
                await unitOfWork.SaveChangesAsync(ct);
                return;
            }

            log.MarkSent(rendered.Subject, timeProvider.GetUtcNow());
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            log.MarkFailed(rendered.Subject, "Reconfirmation delivery interrupted.", timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            log.MarkFailed(rendered.Subject, ex.Message, timeProvider.GetUtcNow());
        }

        await unitOfWork.SaveChangesAsync(ct);
    }

    private static async Task<ReconfirmDeliveryState.Allowed?> GetCurrentAdmissionAsync(
        TeamId teamId,
        TicketedEventId eventId,
        RegistrationListItemDto candidate,
        IRegistrationsFacade registrationsFacade,
        IEmailWriteStore writeStore,
        DateTimeOffset deliveryStart,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        var currentNow = timeProvider.GetUtcNow();
        var query = new ReconfirmDeliveryQuery(
            candidate.RegistrationId,
            candidate.RegistrationCycleId,
            candidate.TicketTypeIds,
            currentNow);
        var state = await registrationsFacade.GetReconfirmDeliveryStateAsync(
            teamId.Value,
            eventId.Value,
            query,
            ct);

        // Window and quiet-hours are start gates. Other authoritative guards
        // (event lifecycle, registration state/cycle, and ticket selection)
        // remain delivery-time guards.
        if (state is ReconfirmDeliveryState.Suppressed suppressed
            && suppressed.Reason is ReconfirmDeliverySuppression.OutsideWindow
                or ReconfirmDeliverySuppression.QuietHours)
        {
            state = await registrationsFacade.GetReconfirmDeliveryStateAsync(
                teamId.Value,
                eventId.Value,
                query with { Now = deliveryStart },
                ct);
        }

        if (state is not ReconfirmDeliveryState.Allowed allowed)
            return null;

        var registrationId = RegistrationId.From(candidate.RegistrationId);
        var registrationCycleId = RegistrationCycleId.From(candidate.RegistrationCycleId);
        var logs = await writeStore.EmailLog
            .AsNoTracking()
            .Where(log => log.TeamId == teamId
                && log.TicketedEventId == eventId
                && log.RegistrationId == registrationId
                && log.RegistrationCycleId == registrationCycleId
                && log.EmailType == BuiltInEmailTemplateNames.Reconfirmation
                && (log.Status == EmailLogStatus.Sent || log.Status == EmailLogStatus.Delivered)
                && log.SentAt.HasValue
                && log.SentAt >= candidate.CreatedAt)
            .Select(log => log.SentAt!.Value)
            .ToListAsync(ct);
        var lastSentAt = logs.Count == 0 ? (DateTimeOffset?)null : logs.Max();
        var baseline = lastSentAt.HasValue && lastSentAt.Value > allowed.RegistrationCreatedAt
            ? lastSentAt.Value
            : allowed.RegistrationCreatedAt;
        if (baseline + allowed.MinimumEmailInterval > currentNow)
            return null;

        return allowed.EffectiveMaxReconfirmationEmails is null
            || logs.Count < allowed.EffectiveMaxReconfirmationEmails.Value
            ? allowed
            : null;
    }

    private static async Task<bool> SendWithInlineRetriesAsync(
        ISmtpBatchSession session,
        EmailMessage message,
        IOptionsMonitor<EmailDeliveryOptions> options,
        Func<CancellationToken, Task<bool>> admissionCheck,
        CancellationToken ct)
    {
        Exception? lastException = null;
        for (var attempt = 0; attempt <= options.CurrentValue.InlineRetryCount; attempt++)
        {
            if (attempt > 0 && options.CurrentValue.InlineRetryDelay > TimeSpan.Zero)
                await Task.Delay(options.CurrentValue.InlineRetryDelay, ct);

            try
            {
                if (!await admissionCheck(ct))
                    return false;

                await session.SendAsync(message, ct);
                return true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }

        throw lastException ?? new InvalidOperationException("SMTP delivery failed.");
    }

    private static async Task EvaluatePolicyCloseAsync(
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
            .AnyAsync(e => e.TeamId == policy.TeamId
                && e.TicketedEventId == policy.TicketedEventId
                && e.ClosesAt == closesAt, ct);
        if (alreadyEvaluated)
            return;

        var (candidates, sentReconfirmationLogs) = await LoadCandidatesAndLogsAsync(
            policy, writeStore, registrationsFacade, ct);
        var autoCancelCandidates = candidates
            .Where(r => r.EffectiveMaxReconfirmationEmails.HasValue
                && GetCurrentCycleLogs(sentReconfirmationLogs, r).Count
                    >= r.EffectiveMaxReconfirmationEmails.Value)
            .ToList();
        if (autoCancelCandidates.Count > 0)
        {
            outbox.Enqueue(new ReconfirmAutoExpiredIntegrationEvent(
                policy.TeamId.Value,
                policy.TicketedEventId.Value,
                autoCancelCandidates.Select(r => r.RegistrationId).ToList(),
                BuildAutoExpiredReferences(autoCancelCandidates)));
        }

        writeStore.ReconfirmPolicyCloseEvaluations.Add(
            ReconfirmPolicyCloseEvaluation.Create(policy.TeamId, policy.TicketedEventId, closesAt, now));
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsPolicyCloseEvaluationReservationViolation(ex))
        {
            // Another hourly evaluator recorded this requested deadline.
        }
    }

    private static async Task<(IReadOnlyList<RegistrationListItemDto> Candidates,
        IReadOnlyList<ReconfirmLogData> SentReconfirmationLogs)> LoadCandidatesAndLogsAsync(
        EventEmailContextView policy,
        IEmailWriteStore writeStore,
        IRegistrationsFacade registrationsFacade,
        CancellationToken ct)
    {
        var candidates = await registrationsFacade.GetRegistrationsAsync(
            policy.TeamId.Value,
            policy.TicketedEventId.Value,
            new QueryRegistrationsDto(RegistrationStatus: RegistrationStatus.Registered, HasReconfirmed: false),
            ct);
        var logs = await writeStore.EmailLog
            .AsNoTracking()
            .Where(log => log.TeamId == policy.TeamId
                && log.TicketedEventId == policy.TicketedEventId
                && log.EmailType == BuiltInEmailTemplateNames.Reconfirmation
                && (log.Status == EmailLogStatus.Sent || log.Status == EmailLogStatus.Delivered)
                && log.SentAt.HasValue)
            .Select(log => new ReconfirmLogData(
                log.RegistrationId,
                log.RegistrationCycleId,
                log.SentAt!.Value))
            .ToListAsync(ct);
        return (candidates, logs);
    }

    private static IReadOnlyList<ReconfirmLogData> GetCurrentCycleLogs(
        IReadOnlyList<ReconfirmLogData> logs,
        RegistrationListItemDto registration) =>
        logs.Where(log => log.RegistrationId == RegistrationId.From(registration.RegistrationId)
            && log.RegistrationCycleId == RegistrationCycleId.From(registration.RegistrationCycleId)
            && log.SentAt >= registration.CreatedAt)
            .ToList();

    private static IReadOnlyList<ReconfirmAutoExpiredRegistrationReference> BuildAutoExpiredReferences(
        IEnumerable<RegistrationListItemDto> registrations) =>
        registrations.Select(r => new ReconfirmAutoExpiredRegistrationReference(
            r.RegistrationId,
            r.RegistrationCycleId,
            r.RegistrationVersion,
            r.TicketCatalogVersion,
            r.TicketTypeIds)).ToList();

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

    private static bool IsQuietHours(EventEmailContextView policy, DateTimeOffset now, TimeZoneInfo timeZone)
    {
        if (!policy.ReconfirmQuietHoursStart.HasValue || !policy.ReconfirmQuietHoursEnd.HasValue)
            return false;
        var localTime = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);
        var start = policy.ReconfirmQuietHoursStart.Value;
        var end = policy.ReconfirmQuietHoursEnd.Value;
        return start < end
            ? localTime >= start && localTime < end
            : localTime >= start || localTime < end;
    }

    private static bool IsPolicyCloseEvaluationReservationViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgresException
        && postgresException.ConstraintName == "PK_reconfirm_policy_close_evaluations";

    private sealed record ReconfirmLogData(
        RegistrationId? RegistrationId,
        RegistrationCycleId? RegistrationCycleId,
        DateTimeOffset SentAt);

    private sealed class RunSmtpSession(IServiceScopeFactory scopeFactory) : IAsyncDisposable
    {
        private ISmtpBatchSession? _session;
        private SmtpTransportSettings? _settings;
        private bool _settingsResolved;

        public async Task<ISmtpBatchSession> GetOrOpenAsync(CancellationToken ct)
        {
            if (_session is not null)
                return _session;

            // Transport is deployment-global and is resolved once, lazily, immediately
            // before the shared SMTP session is opened. This lets policy-close work and
            // its outbox commit survive a missing SMTP configuration.
            if (!_settingsResolved)
            {
                await using var settingsScope = scopeFactory.CreateAsyncScope();
                var settingsResolver = settingsScope.ServiceProvider
                    .GetRequiredService<ISmtpTransportSettingsResolver>();
                _settings = await settingsResolver.ResolveAsync(ct);
                _settingsResolved = true;
            }

            if (_settings is null || !_settings.IsValid())
                throw new InvalidOperationException("Email settings not configured or incomplete.");

            await using var scope = scopeFactory.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISmtpBatchSender>();
            _session = await sender.OpenSessionAsync(_settings, ct);
            return _session;
        }

        public async ValueTask DisposeAsync()
        {
            if (_session is not null)
                await _session.DisposeAsync();
        }
    }
}
