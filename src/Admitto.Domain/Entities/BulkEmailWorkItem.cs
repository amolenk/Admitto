using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Entities;

/// <summary>
/// Represents a job for sending bulk emails.
/// </summary>
public class BulkEmailWorkItem : Aggregate
{
    // EF Core constructor
    private BulkEmailWorkItem()
    {
    }

    private BulkEmailWorkItem(
        Guid id,
        Guid teamId,
        Guid eventId,
        string emailType,
        DateTimeOffset earliestSendTime,
        DateTimeOffset latestSendTime)
        : base(id)
    {
        TeamId = teamId;
        TicketedEventId = eventId;
        EmailType = emailType;
        EarliestSendTime = earliestSendTime;
        LatestSendTime = latestSendTime;
        Status = BulkEmailWorkItemStatus.Pending;
    }

    public Guid TeamId { get; private set; }
    public Guid TicketedEventId { get; private set; }
    public string EmailType { get; private set; } = null!;
    public DateTimeOffset EarliestSendTime { get; private set; }
    public DateTimeOffset LatestSendTime { get; private set; }
    public BulkEmailWorkItemStatus Status { get; private set; }

    public static BulkEmailWorkItem Create(
        Guid teamId,
        Guid eventId,
        string emailType,
        DateTimeOffset earliestSendTime,
        DateTimeOffset latestSendTime)
    {
        return new BulkEmailWorkItem(
            Guid.NewGuid(),
            teamId,
            eventId,
            emailType,
            earliestSendTime,
            latestSendTime);
    }

    public bool TryStart()
    {
        if (Status is BulkEmailWorkItemStatus.Completed or BulkEmailWorkItemStatus.TimedOut)
        {
            return false;
        }
        
        if (LatestSendTime < DateTimeOffset.UtcNow)
        {
            // We're too late, mark as timed out.
            Status = BulkEmailWorkItemStatus.TimedOut;
            return false;
        }

        if (EarliestSendTime >= DateTimeOffset.UtcNow)
        {
            return false;
        }

        Status = BulkEmailWorkItemStatus.Running;
        return true;
    }
    
    public void MarkAsCompleted()
    {
        Status = BulkEmailWorkItemStatus.Completed;
    }
}