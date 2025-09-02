namespace Amolenk.Admitto.Domain.ValueObjects;

public enum BulkEmailWorkItemStatus
{
    Pending = 0,
    PendingRepeat = 1,
    Running = 2,
    Completed = 3,
    Error = 4
}