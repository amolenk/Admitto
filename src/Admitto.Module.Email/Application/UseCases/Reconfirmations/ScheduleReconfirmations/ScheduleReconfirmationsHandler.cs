using Amolenk.Admitto.Module.Email.Application.Jobs;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.Reconfirmations.ScheduleReconfirmations;

/// <summary>
/// Owns the lifecycle of the per-event Quartz trigger that fires
/// <see cref="RequestReconfirmationsJob"/> on the policy cadence (per design D6).
/// </summary>
[RequiresCapability(HostCapability.Jobs | HostCapability.Email)]
internal sealed class ScheduleReconfirmationsHandler(
    ISchedulerFactory schedulerFactory,
    ILogger<ScheduleReconfirmationsHandler> logger)
    : ICommandHandler<ScheduleReconfirmationsCommand>
{
    public const string TriggerGroup = "reconfirm";

    public async ValueTask HandleAsync(
        ScheduleReconfirmationsCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Spec is null)
        {
            await RemoveAsync(command.TicketedEventId, cancellationToken);
        }
        else
        {
            await UpsertAsync(command.TicketedEventId, command.Spec, cancellationToken);
        }
    }

    private async Task UpsertAsync(
        TicketedEventId ticketedEventId,
        Module.Registrations.Contracts.ReconfirmTriggerSpecDto spec,
        CancellationToken cancellationToken)
    {
        if (spec.CadenceDays < 1)
            throw new ArgumentOutOfRangeException(
                nameof(spec), spec.CadenceDays, "Cadence must be at least 1 day.");

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
        var cron = BuildCron(spec.CadenceDays);

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(RequestReconfirmationsJob.Name)
            .UsingJobData(RequestReconfirmationsJob.TeamIdKey, spec.TeamId.ToString())
            .UsingJobData(RequestReconfirmationsJob.TicketedEventIdKey, ticketedEventId.Value.ToString())
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
    /// Maps a cadence in whole days to a cron expression evaluated in the
    /// event's time zone. Daily cadence fires at 09:00 local; multi-day
    /// cadences use day-of-month stepping which approximates "every N days"
    /// within each month boundary — acceptable given the minimum 1-day
    /// cadence and that the cron is the source of truth for tick timing
    /// (per design D5).
    /// </summary>
    private static string BuildCron(int cadenceDays)
    {
        return cadenceDays == 1
            ? "0 0 9 * * ?"
            : $"0 0 9 1/{cadenceDays} * ?";
    }
}
