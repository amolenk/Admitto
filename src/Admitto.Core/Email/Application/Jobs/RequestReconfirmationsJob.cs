using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Amolenk.Admitto.Core.Email.Application.Jobs;

/// <summary>
/// Quartz job fired by the per-event reconfirm trigger. Each tick evaluates
/// which registered, un-reconfirmed attendees are eligible for a reconfirmation
/// email based on the MinEmailInterval throttle (per design D1, D2), then
/// creates one system-triggered <see cref="BulkEmailJob"/> for the eligible set.
/// The cron schedule of the per-event trigger encodes the cadence; this job
/// performs no additional cadence filtering (per design D5).
/// </summary>
[DisallowConcurrentExecution]
internal sealed class RequestReconfirmationsJob(
    IEmailWriteStore writeStore,
    IRegistrationsFacade registrationsFacade,
    [FromKeyedServices(EmailModule.Key)] IOutbox outbox,
    [FromKeyedServices(EmailModule.Key)] IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<RequestReconfirmationsJob> logger)
    : IJob
{
    public const string Name = nameof(RequestReconfirmationsJob);
    public const string TeamIdKey = "TeamId";
    public const string TicketedEventIdKey = "TicketedEventId";
    public const string MinEmailIntervalHoursKey = "MinEmailIntervalHours";

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        var teamIdValue = context.MergedJobDataMap.GetGuidValueFromString(TeamIdKey);
        var eventIdValue = context.MergedJobDataMap.GetGuidValueFromString(TicketedEventIdKey);
        var teamId = TeamId.From(teamIdValue);
        var ticketedEventId = TicketedEventId.From(eventIdValue);

        var minIntervalRaw = context.MergedJobDataMap.GetString(MinEmailIntervalHoursKey);
        int.TryParse(minIntervalRaw, out var minEmailIntervalHours);

        var now = timeProvider.GetUtcNow();
        var candidates = await registrationsFacade.GetRegistrationsAsync(
            ticketedEventId.Value,
            new QueryRegistrationsDto(
                RegistrationStatus: RegistrationStatus.Registered,
                HasReconfirmed: false),
            ct);

        if (candidates.Count == 0)
        {
            logger.LogInformation(
                "Reconfirm tick for event {TicketedEventId}: no un-reconfirmed attendees, skipping.",
                eventIdValue);
            return;
        }

        var emailLogDataByEmail = await writeStore.EmailLog
            .AsNoTracking()
            .Where(l =>
                l.TicketedEventId == ticketedEventId &&
                l.EmailType == BuiltInEmailTemplateNames.Reconfirmation &&
                l.Status == EmailLogStatus.Sent)
            .GroupBy(l => l.Recipient)
            .Select(g => new
            {
                Email = g.Key,
                LastSentAt = g.Max(l => l.SentAt),
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.Email.Value, x => (x.LastSentAt, x.Count), ct);

        var eligibleCandidates = candidates;
        if (minEmailIntervalHours > 0)
        {
            var threshold = TimeSpan.FromHours(minEmailIntervalHours);
            eligibleCandidates = candidates
                .Where(r =>
                {
                    var baseline = emailLogDataByEmail.TryGetValue(r.Email, out var logData) && logData.LastSentAt.HasValue
                        ? (logData.LastSentAt.Value > r.CreatedAt ? logData.LastSentAt.Value : r.CreatedAt)
                        : r.CreatedAt;
                    return baseline + threshold <= now;
                })
                .ToList();

            if (eligibleCandidates.Count == 0)
            {
                logger.LogInformation(
                    "Reconfirm tick for event {TicketedEventId}: all {Total} attendees throttled by MinEmailInterval ({Hours}h), skipping.",
                    eventIdValue,
                    candidates.Count,
                    minEmailIntervalHours);
                return;
            }
        }

        var reconfirmRegistrationIds = eligibleCandidates
            .Where(r =>
                r.EffectiveMaxReconfirmAttempts is null
                || !emailLogDataByEmail.TryGetValue(r.Email, out var logData)
                || logData.Count < r.EffectiveMaxReconfirmAttempts.Value)
            .Select(r => r.RegistrationId)
            .ToList();

        var autoCancelRegistrationIds = eligibleCandidates
            .Where(r =>
                r.EffectiveMaxReconfirmAttempts is not null
                && emailLogDataByEmail.TryGetValue(r.Email, out var logData)
                && logData.Count >= r.EffectiveMaxReconfirmAttempts.Value)
            .Select(r => r.RegistrationId)
            .ToList();

        if (reconfirmRegistrationIds.Count > 0)
        {
            logger.LogInformation(
                "Reconfirm tick for event {TicketedEventId}: creating bulk-email job for {Eligible} attendees.",
                eventIdValue,
                reconfirmRegistrationIds.Count);

            var filter = new QueryRegistrationsDto(
                RegistrationStatus: RegistrationStatus.Registered,
                HasReconfirmed: false,
                RegistrationIds: reconfirmRegistrationIds);

            var job = BulkEmailJob.CreateSystemTriggered(
                teamId,
                ticketedEventId,
                BuiltInEmailTemplateNames.Reconfirmation,
                subject: null,
                textBody: null,
                htmlBody: null,
                source: new AttendeeSource(filter),
                now: now);

            writeStore.BulkEmailJobs.Add(job);
        }

        if (autoCancelRegistrationIds.Count > 0)
        {
            logger.LogInformation(
                "Reconfirm tick for event {TicketedEventId}: auto-cancelling {Cancelled} attendees.",
                eventIdValue,
                autoCancelRegistrationIds.Count);

            outbox.Enqueue(new ReconfirmAutoExpiredIntegrationEvent(
                ticketedEventId.Value,
                autoCancelRegistrationIds));
        }

        if (reconfirmRegistrationIds.Count == 0 && autoCancelRegistrationIds.Count == 0)
        {
            logger.LogInformation(
                "Reconfirm tick for event {TicketedEventId}: no attendees eligible after policy evaluation, skipping.",
                eventIdValue);
            return;
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
