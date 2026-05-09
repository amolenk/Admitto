using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;

namespace Amolenk.Admitto.Core.Module.Registrations.Domain.Entities;

public class ActivityLog : Entity<ActivityLogId>
{
    // Required for EF Core
    private ActivityLog() { }

    private ActivityLog(
        ActivityLogId id,
        Guid registrationId,
        ActivityType activityType,
        DateTimeOffset occurredAt,
        string? metadata)
        : base(id)
    {
        RegistrationId = registrationId;
        ActivityType = activityType;
        OccurredAt = occurredAt;
        Metadata = metadata;
    }

    public Guid RegistrationId { get; private set; }
    public ActivityType ActivityType { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string? Metadata { get; private set; }

    public static ActivityLog Create(
        Guid registrationId,
        ActivityType activityType,
        DateTimeOffset occurredAt,
        string? metadata = null)
    {
        return new ActivityLog(
            ActivityLogId.New(),
            registrationId,
            activityType,
            occurredAt,
            metadata);
    }
}
