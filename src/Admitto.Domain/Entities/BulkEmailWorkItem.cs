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
        BulkEmailWorkItemRepeat? repeat)
        : base(id)
    {
        TeamId = teamId;
        TicketedEventId = eventId;
        EmailType = emailType;
        Repeat = repeat;
        Status = BulkEmailWorkItemStatus.Pending;
    }

    public Guid TeamId { get; private set; }
    public Guid TicketedEventId { get; private set; }
    public string EmailType { get; private set; } = null!;
    public BulkEmailWorkItemRepeat? Repeat { get; private set; }
    public BulkEmailWorkItemStatus Status { get; private set; }
    public DateTimeOffset? LastRunAt { get; private set; }
    public string? Error { get; private set; }

    public static BulkEmailWorkItem Create(
        Guid teamId,
        Guid eventId,
        string emailType,
        BulkEmailWorkItemRepeat? repeat)
    {
        return new BulkEmailWorkItem(
            Guid.NewGuid(),
            teamId,
            eventId,
            emailType,
            repeat);
    }

    public bool TryStart(DateTimeOffset now)
    {
        if (Status is BulkEmailWorkItemStatus.Completed)
        {
            return false;
        }

        if (Repeat is not null && Status == BulkEmailWorkItemStatus.PendingRepeat)
        {
            if (now < Repeat.WindowStart)
            {
                return false;
            }

            if (now > Repeat.WindowEnd)
            {
                if (LastRunAt is null)
                {
                    Status = BulkEmailWorkItemStatus.Error;
                    Error = "The job could not be started within the scheduled window.";
                }
                else
                {
                    Status = BulkEmailWorkItemStatus.Completed;
                }
                return false;
            }
        }

        LastRunAt = now;
        Status = BulkEmailWorkItemStatus.Running;
        return true;
    }
    
    public void Complete()
    {
        Status = Repeat is not null ? BulkEmailWorkItemStatus.PendingRepeat : BulkEmailWorkItemStatus.Completed;
    }

    public void Fail(Exception exception)
    {
        Error = exception.Message;
        Status = BulkEmailWorkItemStatus.Error;
    }
}