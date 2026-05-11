using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Amolenk.Admitto.Core.Email.Application.Jobs;

/// <summary>
/// Quartz job fired by the per-event reconfirm trigger. Each tick creates one
/// system-triggered <see cref="BulkEmailJob"/> with an
/// <see cref="AttendeeSource"/> targeting registered, un-reconfirmed attendees.
/// The cron schedule of the per-event trigger encodes the cadence; this job
/// performs no additional cadence filtering (per design D5).
/// </summary>
[DisallowConcurrentExecution]
internal sealed class RequestReconfirmationsJob(
    IEmailWriteStore writeStore,
    [FromKeyedServices(EmailModule.Key)] IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<RequestReconfirmationsJob> logger)
    : IJob
{
    public const string Name = nameof(RequestReconfirmationsJob);
    public const string TeamIdKey = "TeamId";
    public const string TicketedEventIdKey = "TicketedEventId";

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        var teamIdValue = context.MergedJobDataMap.GetGuidValueFromString(TeamIdKey);
        var eventIdValue = context.MergedJobDataMap.GetGuidValueFromString(TicketedEventIdKey);
        var teamId = TeamId.From(teamIdValue);
        var ticketedEventId = TicketedEventId.From(eventIdValue);

        logger.LogInformation(
            "Reconfirm tick for event {TicketedEventId} (team {TeamId}); creating bulk-email job.",
            eventIdValue, teamIdValue);

        var filter = new QueryRegistrationsDto(
            RegistrationStatus: RegistrationStatus.Registered,
            HasReconfirmed: false);

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
