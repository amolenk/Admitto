using System.Text.Json;
using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Sending;
using Amolenk.Admitto.Core.Email.Application.Sending.Bulk;
using Amolenk.Admitto.Core.Email.Application.Sending.Settings;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.Extensions.Options;
using Npgsql;
using Quartz;

namespace Amolenk.Admitto.Core.Email.Application.Jobs;

/// <summary>
/// Quartz job that sends one bulk-email job. Behaviour per design D3/D4/D9:
/// <list type="bullet">
///   <item>Pending → Resolving → Sending happens in one transaction (snapshot frozen).</item>
///   <item>One <see cref="IBulkSmtpSender"/> session per worker pickup serves every recipient.</item>
///   <item>Cooperative cancellation polled before and after each send.</item>
///   <item>Resume-safe: re-runs only process <see cref="BulkEmailRecipientStatus.Pending"/> recipients.</item>
///   <item>Per-recipient writes use idempotency key <c>bulk:{jobId}:{email}</c> and
///         survive duplicate-row races via the
///         <c>IX_email_log_event_recipient_idempotency</c> unique index.</item>
/// </list>
/// Per-job concurrency isolation is achieved by scheduling one Quartz job per
/// <see cref="BulkEmailJob"/> with a unique <see cref="JobKey"/>; the
/// <see cref="DisallowConcurrentExecutionAttribute"/> then blocks parallel
/// pickups of the same job (the create endpoint in section 5 wires this up).
/// </summary>
[DisallowConcurrentExecution]
internal sealed class SendBulkEmailJob(
    IEmailWriteStore writeStore,
    IBulkEmailRecipientResolver recipientResolver,
    IRegistrationsFacade registrationsFacade,
    IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto> eventContextQuery,
    IEffectiveEmailSettingsResolver settingsResolver,
    IEmailTemplateService templateService,
    IEmailRenderer renderer,
    IBulkSmtpSender bulkSmtpSender,
    [FromKeyedServices(EmailModule.Key)] IUnitOfWork unitOfWork,
    IOptionsMonitor<BulkEmailOptions> options,
    ILogger<SendBulkEmailJob> logger,
    TimeProvider timeProvider)
    : IJob
{
    public const string Name = nameof(SendBulkEmailJob);
    public const string BulkEmailJobIdKey = "BulkEmailJobId";
    public const string TeamIdKey = "TeamId";
    public const string TicketedEventIdKey = "TicketedEventId";

    private static readonly JsonSerializerOptions ParametersJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var bulkJobIdValue = context.MergedJobDataMap.GetGuidValueFromString(BulkEmailJobIdKey);
        var teamIdValue = context.MergedJobDataMap.GetGuidValueFromString(TeamIdKey);
        var eventIdValue = context.MergedJobDataMap.GetGuidValueFromString(TicketedEventIdKey);
        var bulkJobId = BulkEmailJobId.From(bulkJobIdValue);
        var teamId = TeamId.From(teamIdValue);
        var ticketedEventId = TicketedEventId.From(eventIdValue);

        try
        {
            var job = await writeStore.BulkEmailJobs
                .FirstOrDefaultAsync(
                    j => j.Id == bulkJobId && j.TeamId == teamId && j.TicketedEventId == ticketedEventId,
                    ct);

            if (job is null)
            {
                logger.LogWarning("Bulk-email job {BulkEmailJobId} not found; skipping.", bulkJobIdValue);
                return;
            }

            if (job.Status is BulkEmailJobStatus.Completed
                or BulkEmailJobStatus.PartiallyFailed
                or BulkEmailJobStatus.Failed
                or BulkEmailJobStatus.Cancelled)
            {
                logger.LogInformation(
                    "Bulk-email job {BulkEmailJobId} already terminal ({Status}); skipping.",
                    bulkJobIdValue, job.Status);
                return;
            }

            if (job.Status == BulkEmailJobStatus.Pending
                && job.EmailType == BuiltInEmailTemplateNames.Reconfirmation
                && !HasExpectedRegistrationCycles(job.AttendeeFilter))
            {
                job.Fail("Reconfirmation job has no expected registration cycles.", DateTimeOffset.UtcNow);
                await unitOfWork.SaveChangesAsync(ct);
                return;
            }

            // Phase 1: snapshot recipients (Pending → Resolving → Sending) in
            // one transaction. Resume pickups skip this branch.
            if (job.Status == BulkEmailJobStatus.Pending)
            {
                job.BeginResolving(DateTimeOffset.UtcNow);

                IReadOnlyList<BulkEmailRecipient> recipients;
                try
                {
                    recipients = await recipientResolver.ResolveAsync(job.TeamId, job.TicketedEventId, job.AttendeeFilter, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to resolve recipients for bulk-email job {BulkEmailJobId}", bulkJobIdValue);
                    job.Fail($"Recipient resolution failed: {ex.Message}", DateTimeOffset.UtcNow);
                    await unitOfWork.SaveChangesAsync(ct);
                    return;
                }

                job.BeginSending(recipients);

                if (recipients.Count == 0)
                {
                    job.Complete(DateTimeOffset.UtcNow);
                    await unitOfWork.SaveChangesAsync(ct);
                    return;
                }

                await unitOfWork.SaveChangesAsync(ct);
            }

            var pending = job.Recipients
                .Where(r => r.Status == BulkEmailRecipientStatus.Pending)
                .ToList();

            // Authoritative Registrations checks happen before SMTP settings,
            // content rendering, and session creation. A queued reconfirmation
            // job therefore cannot fail into a stale send when event policy or
            // attendee state changed after the snapshot was made.
            if (job.EmailType == BuiltInEmailTemplateNames.Reconfirmation)
            {
                await SuppressIneligibleReconfirmRecipientsAsync(job, pending, ct);
                pending = job.Recipients
                    .Where(r => r.Status == BulkEmailRecipientStatus.Pending)
                    .ToList();
            }

            if (pending.Count == 0)
            {
                job.Complete(timeProvider.GetUtcNow());
                await unitOfWork.SaveChangesAsync(ct);
                return;
            }

            // Resolve effective settings only once an eligible recipient exists;
            // without them we can't open an SMTP session.
            var settings = await settingsResolver.ResolveAsync(job.TeamId, job.TicketedEventId, ct);
            if (settings is null || !settings.IsValid())
            {
                job.Fail("Email settings not configured or incomplete.", timeProvider.GetUtcNow());
                await unitOfWork.SaveChangesAsync(ct);
                return;
            }

            // Phase 2: stream the snapshot through a single SMTP session.
            // Custom bulk emails render job-owned content; system bulk emails
            // (for example reconfirm) render code-owned built-in content.
            EmailTemplate template;
            EventEmailContextDto eventContext;
            try
            {
                eventContext = await eventContextQuery.HandleAsync(
                    new GetEventEmailRenderingContextQuery(
                        job.TeamId,
                        job.TicketedEventId,
                        RegistrationId: null),
                    ct);

                template = job.Subject is not null && job.TextBody is not null && job.HtmlBody is not null
                    ? new EmailTemplate(
                        job.EmailType,
                        job.Subject,
                        job.TextBody,
                        job.HtmlBody)
                    : await templateService.LoadAsync(job.EmailType, job.TeamId, job.TicketedEventId, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to prepare content for bulk-email job {BulkEmailJobId}",
                    bulkJobIdValue);
                job.Fail($"Content preparation failed: {ex.Message}", DateTimeOffset.UtcNow);
                await unitOfWork.SaveChangesAsync(ct);
                return;
            }

            // Skip opening an SMTP session entirely when cancellation was
            // requested before pickup or there is nothing left to send.
            var cancelledBeforeOpen = await IsCancellationRequestedAsync(bulkJobId, teamId, ticketedEventId, ct);
            if (!cancelledBeforeOpen && pending.Count > 0)
            {
                await using var session = await bulkSmtpSender.OpenSessionAsync(settings, ct);
                foreach (var recipient in pending)
                {
                    if (await IsCancellationRequestedAsync(bulkJobId, teamId, ticketedEventId, ct))
                        break;

                    await ProcessRecipientAsync(job, recipient, template, settings, eventContext, session, ct);

                    if (await IsCancellationRequestedAsync(bulkJobId, teamId, ticketedEventId, ct))
                        break;

                    await Task.Delay(options.CurrentValue.PerMessageDelay, ct);
                }
            }

            // Phase 3: terminal state.
            var freshCancellation = await IsCancellationRequestedAsync(bulkJobId, teamId, ticketedEventId, ct);
            if (freshCancellation)
            {
                job.FinaliseCancelled(DateTimeOffset.UtcNow);
            }
            else
            {
                job.Complete(DateTimeOffset.UtcNow);
            }

            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Worker shutting down — let Quartz reschedule on next pickup.
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bulk-email fan-out job {BulkEmailJobId} threw", bulkJobIdValue);
            throw new JobExecutionException(ex);
        }
    }

    private async Task ProcessRecipientAsync(
        BulkEmailJob job,
        BulkEmailRecipient recipient,
        EmailTemplate template,
        EffectiveEmailSettings settings,
        EventEmailContextDto eventContext,
        IBulkSmtpSession session,
        CancellationToken ct)
    {
        var idempotencyKey = $"bulk:{job.Id.Value:N}:{recipient.Email.Value.ToLowerInvariant()}";
        var now = timeProvider.GetUtcNow();
        EmailLog? log = null;

        try
        {
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                recipient.ParametersJson, ParametersJsonOptions) ?? new Dictionary<string, object?>();
            var expectedTicketTypeIds = ReadTicketTypeIds(parameters);

            if (job.EmailType == BuiltInEmailTemplateNames.Reconfirmation
                && !await IsCurrentReconfirmRecipientAsync(job, recipient, expectedTicketTypeIds, ct))
            {
                job.RecordCancelledRecipient(recipient.Email.Value);
                await unitOfWork.SaveChangesAsync(ct);
                return;
            }

            parameters["accent_color"] = settings.AccentColor.Value;
            parameters["font_family"] = settings.FontFamily.Value;
            parameters["team_name"] = eventContext.TeamName;
            parameters["event_name"] = eventContext.EventName;
            parameters["event_website"] = eventContext.WebsiteUrl;
            parameters["public_event_link"] = eventContext.PublicEventLink;
            parameters["register_link"] = eventContext.RegisterLink;
            parameters["reconfirm_link"] = BuildRegistrationLink(eventContext.PublicEventLink, "reconfirm", recipient.RegistrationId);
            parameters["cancel_link"] = BuildRegistrationLink(eventContext.PublicEventLink, "cancel", recipient.RegistrationId);
            parameters["edit_registration_link"] = BuildRegistrationLink(eventContext.PublicEventLink, "edit", recipient.RegistrationId);
            parameters["qrcode_link"] = BuildRegistrationLink(eventContext.PublicEventLink, "qr-code", recipient.RegistrationId);

            var rendered = renderer.Render(
                template,
                parameters,
                subjectOverride: null,
                textBodyOverride: null,
                htmlBodyOverride: null);

            var message = new EmailMessage(
                RecipientAddress: recipient.Email.Value,
                RecipientName: recipient.DisplayName,
                Subject: rendered.Subject,
                TextBody: rendered.TextBody,
                HtmlBody: rendered.HtmlBody);

            log = await ClaimRecipientAsync(job, recipient, idempotencyKey, rendered.Subject, now, ct);
            if (log.Status is EmailLogStatus.Sent or EmailLogStatus.Delivered)
            {
                job.RecordSentRecipient(recipient.Email.Value);
                await unitOfWork.SaveChangesAsync(ct);
                return;
            }

            if (log.Status is EmailLogStatus.Failed or EmailLogStatus.Bounced)
            {
                job.RecordFailedRecipient(recipient.Email.Value, log.LastError ?? "Previous delivery attempt failed.");
                await unitOfWork.SaveChangesAsync(ct);
                return;
            }

            var delivered = await SendWithInlineRetriesAsync(
                session,
                message,
                ct,
                job.EmailType == BuiltInEmailTemplateNames.Reconfirmation
                    ? admissionToken => GetCurrentReconfirmAdmissionAsync(
                        job,
                        recipient,
                        expectedTicketTypeIds,
                        admissionToken)
                    : null);

            if (!delivered)
            {
                writeStore.EmailLog.Remove(log);
                job.RecordCancelledRecipient(recipient.Email.Value);
                await unitOfWork.SaveChangesAsync(ct);
                return;
            }

            log.MarkSent(rendered.Subject, now);
            job.RecordSentRecipient(recipient.Email.Value);

            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Bulk-email job {BulkEmailJobId} failed to send to {Recipient}",
                job.Id.Value, recipient.Email);

            if (log is null)
            {
                log = EmailLog.Create(
                    teamId: job.TeamId,
                    ticketedEventId: job.TicketedEventId,
                    idempotencyKey: idempotencyKey,
                    recipient: recipient.Email,
                    emailType: job.EmailType,
                    subject: job.Subject ?? string.Empty,
                    status: EmailLogStatus.Failed,
                    sentAt: null,
                    statusUpdatedAt: now,
                    lastError: ex.Message,
                    bulkEmailJobId: job.Id,
                    registrationId: recipient.RegistrationId,
                    registrationCycleId: recipient.RegistrationCycleId);

                writeStore.EmailLog.Add(log);
            }
            else
            {
                log.MarkFailed(job.Subject ?? string.Empty, ex.Message, now);
            }

            job.RecordFailedRecipient(recipient.Email.Value, ex.Message);

            await unitOfWork.SaveChangesAsync(ct);
        }
    }

    private async Task SuppressIneligibleReconfirmRecipientsAsync(
        BulkEmailJob job,
        IReadOnlyList<BulkEmailRecipient> pending,
        CancellationToken cancellationToken)
    {
        foreach (var recipient in pending)
        {
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                recipient.ParametersJson, ParametersJsonOptions) ?? new Dictionary<string, object?>();
            var expectedTicketTypeIds = ReadTicketTypeIds(parameters);

            if (await IsCurrentReconfirmRecipientAsync(
                    job, recipient, expectedTicketTypeIds, cancellationToken))
                continue;

            job.RecordCancelledRecipient(recipient.Email.Value);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<EmailLog> ClaimRecipientAsync(
        BulkEmailJob job,
        BulkEmailRecipient recipient,
        string idempotencyKey,
        string subject,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var existing = await FindRecipientLogAsync(job, recipient, idempotencyKey, ct);
        if (existing is not null)
            return existing;

        var log = EmailLog.Create(
            teamId: job.TeamId,
            ticketedEventId: job.TicketedEventId,
            idempotencyKey: idempotencyKey,
            recipient: recipient.Email,
            emailType: job.EmailType,
            subject: subject,
            status: EmailLogStatus.Pending,
            sentAt: null,
            statusUpdatedAt: now,
            bulkEmailJobId: job.Id,
            registrationId: recipient.RegistrationId,
            registrationCycleId: recipient.RegistrationCycleId);

        writeStore.EmailLog.Add(log);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            return log;
        }
        catch (DbUpdateException dbEx) when (IsEmailLogIdempotencyViolation(dbEx))
        {
            Detach(log);
            return await GetRecipientLogAsync(job, recipient, idempotencyKey, ct);
        }
    }

    private static string BuildRegistrationLink(string publicEventLink, string action, RegistrationId registrationId) =>
        $"{publicEventLink}/{action}/{registrationId.Value}";

    private async Task<EmailLog> GetRecipientLogAsync(
        BulkEmailJob job,
        BulkEmailRecipient recipient,
        string idempotencyKey,
        CancellationToken ct)
    {
        return await FindRecipientLogAsync(job, recipient, idempotencyKey, ct)
               ?? throw new InvalidOperationException(
                   $"Email log claim not found for bulk-email job {job.Id.Value} recipient {recipient.Email.Value}.");
    }

    private async Task<EmailLog?> FindRecipientLogAsync(
        BulkEmailJob job,
        BulkEmailRecipient recipient,
        string idempotencyKey,
        CancellationToken ct)
    {
        return await writeStore.EmailLog.FirstOrDefaultAsync(
            log => log.TicketedEventId == job.TicketedEventId
                   && log.Recipient == recipient.Email
                   && log.IdempotencyKey == idempotencyKey,
            ct);
    }

    private void Detach(EmailLog log)
    {
        if (writeStore is DbContext dbContext)
            dbContext.Entry(log).State = EntityState.Detached;
    }

    private async Task<bool> IsCancellationRequestedAsync(
        BulkEmailJobId jobId,
        TeamId teamId,
        TicketedEventId ticketedEventId,
        CancellationToken ct)
    {
        return await writeStore.BulkEmailJobs
            .Where(j => j.Id == jobId && j.TeamId == teamId && j.TicketedEventId == ticketedEventId)
            .Select(j => j.CancellationRequestedAt)
            .FirstOrDefaultAsync(ct) is not null;
    }

    private static bool HasExpectedRegistrationCycles(BulkEmailAttendeeFilter filter) =>
        filter.RegistrationIds is not { Count: > 0 }
        || (filter.RegistrationCycleIds is { } registrationCycles
            && filter.RegistrationIds.All(registrationCycles.ContainsKey));

    private async Task<bool> IsCurrentReconfirmRecipientAsync(
        BulkEmailJob job,
        BulkEmailRecipient recipient,
        IReadOnlyCollection<Guid> expectedTicketTypeIds,
        CancellationToken cancellationToken)
        => await GetCurrentReconfirmAdmissionAsync(
            job, recipient, expectedTicketTypeIds, cancellationToken) is not null;

    private async Task<ReconfirmDeliveryState.Allowed?> GetCurrentReconfirmAdmissionAsync(
        BulkEmailJob job,
        BulkEmailRecipient recipient,
        IReadOnlyCollection<Guid> expectedTicketTypeIds,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var state = await registrationsFacade.GetReconfirmDeliveryStateAsync(
            job.TeamId.Value,
            job.TicketedEventId.Value,
            new ReconfirmDeliveryQuery(
                recipient.RegistrationId.Value,
                recipient.RegistrationCycleId?.Value ?? Guid.Empty,
                expectedTicketTypeIds,
                now),
            cancellationToken);

        if (state is not ReconfirmDeliveryState.Allowed allowed
            || now >= allowed.DeliveryCutoffAt)
            return null;

        var sentLogs = await writeStore.EmailLog
            .AsNoTracking()
            .Where(log =>
                log.TeamId == job.TeamId
                && log.TicketedEventId == job.TicketedEventId
                && log.RegistrationId == recipient.RegistrationId
                && log.RegistrationCycleId == recipient.RegistrationCycleId
                && log.EmailType == BuiltInEmailTemplateNames.Reconfirmation
                && (log.Status == EmailLogStatus.Sent || log.Status == EmailLogStatus.Delivered)
                && log.SentAt.HasValue)
            .Select(log => log.SentAt!.Value)
            .ToListAsync(cancellationToken);
        DateTimeOffset? lastSentAt = sentLogs.Count == 0 ? null : sentLogs.Max();
        var baseline = lastSentAt.HasValue && lastSentAt.Value > allowed.RegistrationCreatedAt
            ? lastSentAt.Value
            : allowed.RegistrationCreatedAt;
        if (baseline + allowed.MinimumEmailInterval > now)
            return null;

        if (allowed.EffectiveMaxReconfirmationEmails is null)
            return allowed;

        return sentLogs.Count < allowed.EffectiveMaxReconfirmationEmails.Value ? allowed : null;
    }

    private static IReadOnlyCollection<Guid> ReadTicketTypeIds(
        IReadOnlyDictionary<string, object?> parameters)
    {
        if (!parameters.TryGetValue("ticket_type_ids", out var raw)
            || raw is not JsonElement element
            || element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return element.EnumerateArray().Select(value => value.GetGuid()).ToArray();
    }

    private static bool IsEmailLogIdempotencyViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg
           && pg.ConstraintName == "IX_email_log_event_recipient_idempotency";

    private async Task<bool> SendWithInlineRetriesAsync(
        IBulkSmtpSession session,
        EmailMessage message,
        CancellationToken ct,
        Func<CancellationToken, Task<ReconfirmDeliveryState.Allowed?>>? admissionCheck = null)
    {
        Exception? lastException = null;
        for (var attempt = 0; attempt <= options.CurrentValue.InlineRetryCount; attempt++)
        {
            if (attempt > 0 && options.CurrentValue.InlineRetryDelay > TimeSpan.Zero)
                await Task.Delay(options.CurrentValue.InlineRetryDelay, ct);

            DeliveryCutoffCancellation? cutoffCancellation = null;
            try
            {
                var admission = admissionCheck is null ? null : await admissionCheck(ct);
                if (admissionCheck is not null && admission is null)
                    return false;

                if (admission is not null)
                {
                    cutoffCancellation = new DeliveryCutoffCancellation(
                        timeProvider,
                        admission.DeliveryCutoffAt,
                        ct);
                    if (cutoffCancellation.CutoffReached)
                        return false;
                }

                await session.SendAsync(message, cutoffCancellation?.Token ?? ct);
                return true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException) when (cutoffCancellation?.CutoffReached == true)
            {
                return false;
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
            finally
            {
                cutoffCancellation?.Dispose();
            }
        }

        throw lastException ?? new InvalidOperationException("SMTP delivery failed.");
    }

    private sealed class DeliveryCutoffCancellation : IDisposable
    {
        private static readonly TimeSpan MaximumTimerDelay =
            TimeSpan.FromMilliseconds(uint.MaxValue - 1L);

        private readonly TimeProvider _timeProvider;
        private readonly DateTimeOffset _cutoff;
        private readonly CancellationTokenSource _source;
        private ITimer? _timer;
        private int _cutoffReached;

        public DeliveryCutoffCancellation(
            TimeProvider timeProvider,
            DateTimeOffset cutoff,
            CancellationToken callerToken)
        {
            _timeProvider = timeProvider;
            _cutoff = cutoff;
            _source = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
            var delay = cutoff - timeProvider.GetUtcNow();
            if (delay <= TimeSpan.Zero)
            {
                ReachCutoff();
                return;
            }

            _timer = timeProvider.CreateTimer(
                static state => ((DeliveryCutoffCancellation)state!).ReachCutoff(),
                this,
                TimerDelay(delay),
                Timeout.InfiniteTimeSpan);
        }

        public CancellationToken Token => _source.Token;
        public bool CutoffReached => Volatile.Read(ref _cutoffReached) == 1;

        public void Dispose()
        {
            _timer?.Dispose();
            _source.Dispose();
        }

        private void ReachCutoff()
        {
            var remaining = _cutoff - _timeProvider.GetUtcNow();
            if (remaining > TimeSpan.Zero)
            {
                _timer?.Change(TimerDelay(remaining), Timeout.InfiniteTimeSpan);
                return;
            }

            Interlocked.Exchange(ref _cutoffReached, 1);
            _source.Cancel();
        }

        private static TimeSpan TimerDelay(TimeSpan remaining) =>
            remaining > MaximumTimerDelay ? MaximumTimerDelay : remaining;
    }
}
