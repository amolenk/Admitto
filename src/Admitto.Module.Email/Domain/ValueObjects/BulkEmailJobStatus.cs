namespace Amolenk.Admitto.Module.Email.Domain.ValueObjects;

public enum BulkEmailJobStatus
{
    Pending = 0,
    Resolving = 1,
    Sending = 2,
    Completed = 3,
    PartiallyFailed = 4,
    Failed = 5,
    Cancelled = 6
}
