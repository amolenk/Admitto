using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.Projections.ActivityLog;

public class ActivityLogView
{
    // Required for EF Core
    private ActivityLogView() { }

    private ActivityLogView(
        Guid id,
        Guid teamId,
        Guid eventId,
        Guid registrationId,
        ActivityType activityType,
        DateTimeOffset occurredAt,
        string? metadata)
    {
        Id = id;
        TeamId = teamId;
        EventId = eventId;
        RegistrationId = registrationId;
        ActivityType = activityType;
        OccurredAt = occurredAt;
        Metadata = metadata;
    }

    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid RegistrationId { get; private set; }
    public ActivityType ActivityType { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string? Metadata { get; private set; }

    public static ActivityLogView Create(
        Guid teamId,
        Guid eventId,
        Guid registrationId,
        ActivityType activityType,
        DateTimeOffset occurredAt,
        string? metadata = null)
    {
        return new ActivityLogView(
            Guid.NewGuid(),
            teamId,
            eventId,
            registrationId,
            activityType,
            occurredAt,
            metadata);
    }
}
