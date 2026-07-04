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
    IQueryHandler<GetEventEmailRenderingContextQuery, EventEmailContextDto> eventContextQuery,
    IEffectiveEmailSettingsResolver settingsResolver,
    IEmailTemplateService templateService,
    IEmailRenderer renderer,
    IBulkSmtpSender bulkSmtpSender,
    [FromKeyedServices(EmailModule.Key)] IUnitOfWork unitOfWork,
    IOptionsMonitor<BulkEmailOptions> options,
    ILogger<SendBulkEmailJob> logger)
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

            // Resolve effective settings up front; without them we can't open
            // an SMTP session and the job is unrecoverable until reconfigured.
            var settings = await settingsResolver.ResolveAsync(job.TeamId, job.TicketedEventId, ct);
            if (settings is null || !settings.IsValid())
            {
                job.Fail("Email settings not configured or incomplete.", DateTimeOffset.UtcNow);
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
                    recipients = await recipientResolver.ResolveAsync(job.TeamId, job.TicketedEventId, job.Source, ct);
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
                    ? EmailTemplate.Create(
                        job.TeamId,
                        job.TicketedEventId,
                        job.EmailType,
                        job.Subject ?? throw new InvalidOperationException("Custom bulk email subject is required."),
                        job.TextBody ?? throw new InvalidOperationException("Custom bulk email text body is required."),
                        job.HtmlBody ?? throw new InvalidOperationException("Custom bulk email HTML body is required."))
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

            var pending = job.Recipients
                .Where(r => r.Status == BulkEmailRecipientStatus.Pending)
                .ToList();

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
        var now = DateTimeOffset.UtcNow;
        EmailLog? log = null;

        try
        {
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                recipient.ParametersJson, ParametersJsonOptions) ?? new Dictionary<string, object?>();
            parameters["accent_color"] = eventContext.TeamAccentColor;
            parameters["font_family"] = settings.FontFamily.Value;
            parameters["event_name"] = eventContext.EventName;
            parameters["event_website"] = eventContext.WebsiteUrl;
            parameters["public_event_link"] = eventContext.PublicEventLink;
            parameters["register_link"] = eventContext.RegisterLink;
            parameters["cancel_link"] = BuildRegistrationLink(eventContext.PublicEventLink, "cancel", recipient.RegistrationId);
            parameters["edit_registration_link"] = BuildRegistrationLink(eventContext.PublicEventLink, "edit", recipient.RegistrationId);
            parameters["qr_code_link"] = BuildRegistrationLink(eventContext.PublicEventLink, "qr-code", recipient.RegistrationId);
            parameters["team_accent_color"] = eventContext.TeamAccentColor;

            var rendered = renderer.Render(
                template,
                parameters,
                subjectOverride: null,
                textBodyOverride: null,
                htmlBodyOverride: null);

            var message = new EmailMessage(
                RecipientAddress: recipient.Email.Value,
                RecipientName: recipient.DisplayName ?? recipient.Email.Value,
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

            await SendWithInlineRetriesAsync(session, message, ct);

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
                    registrationId: recipient.RegistrationId);

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
            registrationId: recipient.RegistrationId);

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

    private static string BuildRegistrationLink(string publicEventLink, string action, RegistrationId? registrationId) =>
        registrationId is null
            ? publicEventLink
            : $"{publicEventLink}/{action}/{registrationId.Value.Value}";

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

    private static bool IsEmailLogIdempotencyViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg
           && pg.ConstraintName == "IX_email_log_event_recipient_idempotency";

    private async ValueTask<string?> SendWithInlineRetriesAsync(
        IBulkSmtpSession session,
        EmailMessage message,
        CancellationToken ct)
    {
        Exception? lastException = null;
        for (var attempt = 0; attempt <= options.CurrentValue.InlineRetryCount; attempt++)
        {
            if (attempt > 0 && options.CurrentValue.InlineRetryDelay > TimeSpan.Zero)
                await Task.Delay(options.CurrentValue.InlineRetryDelay, ct);

            try
            {
                return await session.SendAsync(message, ct);
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
}
