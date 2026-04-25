using System.Text.Json;
using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Application.Sending;
using Amolenk.Admitto.Module.Email.Application.Sending.Bulk;
using Amolenk.Admitto.Module.Email.Application.Sending.Settings;
using Amolenk.Admitto.Module.Email.Application.Templating;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Quartz;

namespace Amolenk.Admitto.Module.Email.Application.Jobs;

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
[RequiresCapability(HostCapability.Jobs | HostCapability.Email)]
[DisallowConcurrentExecution]
internal sealed class SendBulkEmailJob(
    IEmailWriteStore writeStore,
    IBulkEmailRecipientResolver recipientResolver,
    IEffectiveEmailSettingsResolver settingsResolver,
    IEmailTemplateService templateService,
    IEmailRenderer renderer,
    IBulkSmtpSender bulkSmtpSender,
    [FromKeyedServices(EmailModuleKey.Value)] IUnitOfWork unitOfWork,
    IOptionsMonitor<BulkEmailOptions> options,
    ILogger<SendBulkEmailJob> logger)
    : IJob
{
    public const string Name = nameof(SendBulkEmailJob);
    public const string BulkEmailJobIdKey = "BulkEmailJobId";

    private static readonly JsonSerializerOptions ParametersJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var bulkJobIdValue = context.MergedJobDataMap.GetGuidValueFromString(BulkEmailJobIdKey);
        var bulkJobId = BulkEmailJobId.From(bulkJobIdValue);

        try
        {
            var job = await writeStore.BulkEmailJobs
                .FirstOrDefaultAsync(j => j.Id == bulkJobId, ct);

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
                    recipients = await recipientResolver.ResolveAsync(job.TicketedEventId, job.Source, ct);
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
            EmailTemplate template;
            try
            {
                template = await templateService.LoadAsync(
                    job.EmailType, job.TeamId, job.TicketedEventId, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to load template '{EmailType}' for bulk-email job {BulkEmailJobId}",
                    job.EmailType, bulkJobIdValue);
                job.Fail($"Template load failed: {ex.Message}", DateTimeOffset.UtcNow);
                await unitOfWork.SaveChangesAsync(ct);
                return;
            }

            var pending = job.Recipients
                .Where(r => r.Status == BulkEmailRecipientStatus.Pending)
                .ToList();

            // Skip opening an SMTP session entirely when cancellation was
            // requested before pickup or there is nothing left to send.
            var cancelledBeforeOpen = await IsCancellationRequestedAsync(bulkJobId, ct);
            if (!cancelledBeforeOpen && pending.Count > 0)
            {
                await using var session = await bulkSmtpSender.OpenSessionAsync(settings, ct);
                foreach (var recipient in pending)
                {
                    if (await IsCancellationRequestedAsync(bulkJobId, ct))
                        break;

                    await ProcessRecipientAsync(job, recipient, template, session, ct);

                    if (await IsCancellationRequestedAsync(bulkJobId, ct))
                        break;

                    await Task.Delay(options.CurrentValue.PerMessageDelay, ct);
                }
            }

            // Phase 3: terminal state.
            var freshCancellation = await IsCancellationRequestedAsync(bulkJobId, ct);
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
        IBulkSmtpSession session,
        CancellationToken ct)
    {
        var idempotencyKey = $"bulk:{job.Id.Value:N}:{recipient.Email.ToLowerInvariant()}";
        var now = DateTimeOffset.UtcNow;

        try
        {
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                recipient.ParametersJson, ParametersJsonOptions) ?? new Dictionary<string, object?>();

            var rendered = renderer.Render(
                template,
                parameters,
                subjectOverride: job.Subject,
                textBodyOverride: job.TextBody,
                htmlBodyOverride: job.HtmlBody);

            var message = new EmailMessage(
                RecipientAddress: recipient.Email,
                RecipientName: recipient.DisplayName ?? recipient.Email,
                Subject: rendered.Subject,
                TextBody: rendered.TextBody,
                HtmlBody: rendered.HtmlBody);

            var providerMessageId = await session.SendAsync(message, ct);

            writeStore.EmailLog.Add(EmailLog.Create(
                teamId: job.TeamId.Value,
                ticketedEventId: job.TicketedEventId.Value,
                idempotencyKey: idempotencyKey,
                recipient: recipient.Email,
                emailType: job.EmailType,
                subject: rendered.Subject,
                provider: bulkSmtpSender.Provider,
                providerMessageId: providerMessageId,
                status: EmailLogStatus.Sent,
                sentAt: now,
                statusUpdatedAt: now,
                bulkEmailJobId: job.Id));

            job.RecordSentRecipient(recipient.Email);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateException dbEx) when (IsEmailLogIdempotencyViolation(dbEx))
            {
                // Another pickup already wrote this row; recipient is in fact Sent.
                logger.LogDebug(
                    "Idempotent skip for bulk-email job {BulkEmailJobId} recipient {Recipient}",
                    job.Id.Value, recipient.Email);
            }
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

            writeStore.EmailLog.Add(EmailLog.Create(
                teamId: job.TeamId.Value,
                ticketedEventId: job.TicketedEventId.Value,
                idempotencyKey: idempotencyKey,
                recipient: recipient.Email,
                emailType: job.EmailType,
                subject: job.Subject ?? string.Empty,
                provider: bulkSmtpSender.Provider,
                providerMessageId: null,
                status: EmailLogStatus.Failed,
                sentAt: null,
                statusUpdatedAt: now,
                lastError: ex.Message,
                bulkEmailJobId: job.Id));

            job.RecordFailedRecipient(recipient.Email, ex.Message);

            try
            {
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateException dbEx) when (IsEmailLogIdempotencyViolation(dbEx))
            {
                // A previous pickup already recorded an outcome — keep going.
            }
        }
    }

    private async Task<bool> IsCancellationRequestedAsync(BulkEmailJobId jobId, CancellationToken ct)
    {
        return await writeStore.BulkEmailJobs
            .Where(j => j.Id == jobId)
            .Select(j => j.CancellationRequestedAt)
            .FirstOrDefaultAsync(ct) is not null;
    }

    private static bool IsEmailLogIdempotencyViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException pg
           && pg.ConstraintName == "IX_email_log_event_recipient_idempotency";
}
