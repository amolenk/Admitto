using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts;
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

        QueryRegistrationsDto filter;

        if (minEmailIntervalHours > 0)
        {
            var now = timeProvider.GetUtcNow();
            var candidates = await registrationsFacade.QueryRegistrationsAsync(
                ticketedEventId,
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

            // Build a map of recipient email → last reconfirmation sent-at.
            var lastSentByEmail = await writeStore.EmailLog
                .AsNoTracking()
                .Where(l =>
                    l.TicketedEventId == ticketedEventId &&
                    l.EmailType == BuiltInEmailTemplateNames.Reconfirmation &&
                    l.Status == EmailLogStatus.Sent)
                .GroupBy(l => l.Recipient)
                .Select(g => new { Email = g.Key, LastSentAt = g.Max(l => l.SentAt) })
                .ToDictionaryAsync(x => x.Email.Value, x => x.LastSentAt, ct);

            var threshold = TimeSpan.FromHours(minEmailIntervalHours);
            var eligibleIds = candidates
                .Where(r =>
                {
                    var baseline = lastSentByEmail.TryGetValue(r.Email, out var lastSent) && lastSent.HasValue
                        ? (lastSent.Value > r.CreatedAt ? lastSent.Value : r.CreatedAt)
                        : r.CreatedAt;
                    return baseline + threshold <= now;
                })
                .Select(r => r.RegistrationId)
                .ToList();

            if (eligibleIds.Count == 0)
            {
                logger.LogInformation(
                    "Reconfirm tick for event {TicketedEventId}: all {Total} attendees throttled by MinEmailInterval ({Hours}h), skipping.",
                    eventIdValue, candidates.Count, minEmailIntervalHours);
                return;
            }

            logger.LogInformation(
                "Reconfirm tick for event {TicketedEventId}: {Eligible}/{Total} attendees eligible after MinEmailInterval throttle ({Hours}h); creating bulk-email job.",
                eventIdValue, eligibleIds.Count, candidates.Count, minEmailIntervalHours);

            filter = new QueryRegistrationsDto(
                RegistrationStatus: RegistrationStatus.Registered,
                HasReconfirmed: false,
                RegistrationIds: eligibleIds);
        }
        else
        {
            logger.LogInformation(
                "Reconfirm tick for event {TicketedEventId} (team {TeamId}); creating bulk-email job.",
                eventIdValue, teamIdValue);

            filter = new QueryRegistrationsDto(
                RegistrationStatus: RegistrationStatus.Registered,
                HasReconfirmed: false);
        }

        var job = BulkEmailJob.CreateSystemTriggered(
            teamId,
            ticketedEventId,
            BuiltInEmailTemplateNames.Reconfirmation,
            subject: null,
            textBody: null,
            htmlBody: null,
            source: new AttendeeSource(filter),
            now: timeProvider.GetUtcNow());

        writeStore.BulkEmailJobs.Add(job);

        await unitOfWork.SaveChangesAsync(ct);
    }
}
