using System.Text.Json;

namespace Amolenk.Admitto.Domain.Entities;

public class ScheduledJob : Entity
{
    public string JobType { get; private set; } = null!;
    public JsonDocument JobData { get; private set; } = null!;
    public string CronExpression { get; private set; } = null!;
    public DateTimeOffset NextRunTime { get; private set; }
    public DateTimeOffset? LastRunTime { get; private set; }
    public bool IsEnabled { get; private set; } = true;

    protected ScheduledJob() { }

    public ScheduledJob(Guid id, string jobType, JsonDocument jobData, string cronExpression, DateTimeOffset nextRunTime) : base(id)
    {
        JobType = jobType;
        JobData = jobData;
        CronExpression = cronExpression;
        NextRunTime = nextRunTime;
    }

    public void UpdateNextRunTime(DateTimeOffset nextRunTime)
    {
        NextRunTime = nextRunTime;
        LastRunTime = DateTimeOffset.UtcNow;
    }

    public void UpdateSchedule(string cronExpression, DateTimeOffset nextRunTime)
    {
        CronExpression = cronExpression;
        NextRunTime = nextRunTime;
    }

    public void Disable()
    {
        IsEnabled = false;
    }

    public void Enable()
    {
        IsEnabled = true;
    }
}