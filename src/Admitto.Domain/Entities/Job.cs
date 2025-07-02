using System.Text.Json;

namespace Amolenk.Admitto.Domain.Entities;

public class Job : Entity
{
    public string JobType { get; private set; } = null!;
    public JsonDocument JobData { get; private set; } = null!;
    public JobStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ProgressMessage { get; private set; }
    public int? ProgressPercent { get; private set; }

    protected Job() { }

    public Job(Guid id, string jobType, JsonDocument jobData) : base(id)
    {
        JobType = jobType;
        JobData = jobData;
        Status = JobStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Start()
    {
        if (Status != JobStatus.Pending)
            throw new InvalidOperationException($"Cannot start job in status {Status}");

        Status = JobStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        if (Status != JobStatus.Running)
            throw new InvalidOperationException($"Cannot complete job in status {Status}");

        Status = JobStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        if (Status != JobStatus.Running)
            throw new InvalidOperationException($"Cannot fail job in status {Status}");

        Status = JobStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
        ErrorMessage = errorMessage;
    }

    public void UpdateProgress(string message, int? percentComplete = null)
    {
        if (Status != JobStatus.Running)
            throw new InvalidOperationException($"Cannot update progress for job in status {Status}");

        ProgressMessage = message;
        ProgressPercent = percentComplete;
    }
}

public enum JobStatus
{
    Pending,
    Running,
    Completed,
    Failed
}