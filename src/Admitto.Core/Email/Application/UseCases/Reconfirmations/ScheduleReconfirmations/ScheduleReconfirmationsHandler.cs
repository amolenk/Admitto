using Amolenk.Admitto.Core.Email.Application.Jobs;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Quartz;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations;

/// <summary>
/// Owns the lifecycle of the per-event Quartz trigger that fires
/// <see cref="RequestReconfirmationsJob"/> on the policy cadence (per design D6).
/// </summary>
internal sealed class ScheduleReconfirmationsHandler(
    ISchedulerFactory schedulerFactory,
    ILogger<ScheduleReconfirmationsHandler> logger)
    : ICommandHandler<ScheduleReconfirmationsCommand>, IWorkerOnly
{
    public const string TriggerGroup = "reconfirm";

    public async ValueTask HandleAsync(
        ScheduleReconfirmationsCommand command,
        CancellationToken cancellationToken)
    {
        var ticketedEventId = TicketedEventId.From(command.TicketedEventId);

        if (command.Spec is null)
        {
            await RemoveAsync(ticketedEventId, cancellationToken);
        }
        else
        {
            await UpsertAsync(ticketedEventId, command.Spec, cancellationToken);
        }
    }

    private async Task UpsertAsync(
        TicketedEventId ticketedEventId,
        ReconfirmTriggerSpecDto spec,
        CancellationToken cancellationToken)
    {
        if (spec.CadenceHours < 1)
            throw new ArgumentOutOfRangeException(
                nameof(spec), spec.CadenceHours, "Cadence must be at least 1 hour.");

        if (spec.ClosesAt <= spec.OpensAt)
            throw new ArgumentException("Window close must be after open.", nameof(spec));

        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(spec.TimeZone);
        }
        catch (TimeZoneNotFoundException ex)
        {
            logger.LogWarning(ex,
                "Skipping reconfirm trigger upsert for event {TicketedEventId}: unknown time zone '{TimeZone}'.",
                ticketedEventId.Value, spec.TimeZone);
            return;
        }

        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var triggerKey = TriggerKeyFor(ticketedEventId);
        var cron = BuildCron(spec.CadenceHours);

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(RequestReconfirmationsJob.Name)
            .UsingJobData(RequestReconfirmationsJob.TeamIdKey, spec.TeamId.ToString())
            .UsingJobData(RequestReconfirmationsJob.TicketedEventIdKey, ticketedEventId.Value.ToString())
            .UsingJobData(RequestReconfirmationsJob.MinEmailIntervalHoursKey, spec.MinEmailIntervalHours.ToString())
            .StartAt(spec.OpensAt)
            .EndAt(spec.ClosesAt)
            .WithCronSchedule(cron, options => options
                .InTimeZone(tz)
                .WithMisfireHandlingInstructionDoNothing())
            .Build();

        var existing = await scheduler.GetTrigger(triggerKey, cancellationToken);
        if (existing is not null)
        {
            await scheduler.RescheduleJob(triggerKey, trigger, cancellationToken);
            logger.LogInformation(
                "Replaced reconfirm trigger for event {TicketedEventId} (cron '{Cron}' in {TimeZone}).",
                ticketedEventId.Value, cron, spec.TimeZone);
        }
        else
        {
            await scheduler.ScheduleJob(trigger, cancellationToken);
            logger.LogInformation(
                "Scheduled reconfirm trigger for event {TicketedEventId} (cron '{Cron}' in {TimeZone}).",
                ticketedEventId.Value, cron, spec.TimeZone);
        }
    }

    private async Task RemoveAsync(TicketedEventId ticketedEventId, CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var triggerKey = TriggerKeyFor(ticketedEventId);

        var removed = await scheduler.UnscheduleJob(triggerKey, cancellationToken);
        if (removed)
        {
            logger.LogInformation(
                "Removed reconfirm trigger for event {TicketedEventId}.",
                ticketedEventId.Value);
        }
    }

    internal static TriggerKey TriggerKeyFor(TicketedEventId ticketedEventId) =>
        new(ticketedEventId.Value.ToString("N"), TriggerGroup);

    /// <summary>
    /// Maps a cadence in whole hours to a Quartz cron expression evaluated in
    /// the event's time zone. Sub-day cadences fire at the top of every Nth
    /// hour; day-level cadences (multiples of 24) fire at 09:00 local using
    /// day-of-month stepping — acceptable given that the cron is the source
    /// of truth for tick timing (per design D5).
    /// </summary>
    private static string BuildCron(int cadenceHours)
    {
        if (cadenceHours < 24)
            return $"0 0 0/{cadenceHours} * * ?";

        int cadenceDays = cadenceHours / 24;
        return cadenceDays == 1
            ? "0 0 9 * * ?"
            : $"0 0 9 1/{cadenceDays} * ?";
    }
}
