using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.Entities;

/// <summary>
/// Authoritative aggregate for a ticketed event in the Registrations module.
/// Owns the slug/name/dates, the lifecycle status, and the three policies
/// (registration, cancellation, reconfirm) as value objects.
/// </summary>
/// <remarks>
/// Slug uniqueness within a team is enforced by the unique index on
/// <c>(TeamId, Slug)</c> defined in the EF configuration. Policy mutators
/// reject when the aggregate is not Active; lifecycle transitions are
/// one-way (Active → Cancelled → Archived, or Active → Archived directly).
/// </remarks>
public class TicketedEvent : Aggregate<TicketedEventId>
{
    // ReSharper disable once UnusedMember.Local — required by EF Core
    private TicketedEvent() { }

    private TicketedEvent(
        TicketedEventId id,
        TeamId teamId,
        Slug slug,
        DisplayName name,
        AbsoluteUrl websiteUrl,
        AbsoluteUrl baseUrl,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        TimeZoneId timeZone)
        : base(id)
    {
        TeamId = teamId;
        Slug = slug;
        Name = name;
        WebsiteUrl = websiteUrl;
        BaseUrl = baseUrl;
        StartsAt = startsAt;
        EndsAt = endsAt;
        TimeZone = timeZone;
        Status = EventLifecycleStatus.Active;
    }

    public TeamId TeamId { get; private set; }
    public Slug Slug { get; private set; }
    public DisplayName Name { get; private set; }
    public AbsoluteUrl WebsiteUrl { get; private set; }
    public AbsoluteUrl BaseUrl { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public TimeZoneId TimeZone { get; private set; }
    public EventLifecycleStatus Status { get; private set; }

    public TicketedEventRegistrationPolicy? RegistrationPolicy { get; private set; }
    public TicketedEventCancellationPolicy? CancellationPolicy { get; private set; }
    public TicketedEventReconfirmPolicy? ReconfirmPolicy { get; private set; }
    public AdditionalDetailSchema AdditionalDetailSchema { get; private set; } = AdditionalDetailSchema.Empty;

    public bool IsActive => Status == EventLifecycleStatus.Active;

    public static TicketedEvent Create(
        TicketedEventId id,
        TeamId teamId,
        Slug slug,
        DisplayName name,
        AbsoluteUrl websiteUrl,
        AbsoluteUrl baseUrl,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        TimeZoneId timeZone)
    {
        if (endsAt < startsAt)
            throw new BusinessRuleViolationException(Errors.EndBeforeStart);

        return new TicketedEvent(id, teamId, slug, name, websiteUrl, baseUrl, startsAt, endsAt, timeZone);
    }

    public void ChangeTimeZone(TimeZoneId timeZone)
    {
        EnsureActive();

        if (TimeZone == timeZone)
            return;

        TimeZone = timeZone;
        AddDomainEvent(new TicketedEventTimeZoneChangedDomainEvent(TeamId, Id, timeZone));
    }

    public void UpdateDetails(
        DisplayName name,
        AbsoluteUrl websiteUrl,
        AbsoluteUrl baseUrl,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt)
    {
        EnsureActive();

        if (endsAt < startsAt)
            throw new BusinessRuleViolationException(Errors.EndBeforeStart);

        Name = name;
        WebsiteUrl = websiteUrl;
        BaseUrl = baseUrl;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    public void Cancel()
    {
        if (Status == EventLifecycleStatus.Cancelled)
            throw new BusinessRuleViolationException(Errors.AlreadyCancelled);

        if (Status == EventLifecycleStatus.Archived)
            throw new BusinessRuleViolationException(Errors.AlreadyArchived);

        Status = EventLifecycleStatus.Cancelled;
        AddDomainEvent(new TicketedEventStatusChangedDomainEvent(Id, TeamId, Slug, Status));
    }

    public void Archive()
    {
        if (Status == EventLifecycleStatus.Archived)
            throw new BusinessRuleViolationException(Errors.AlreadyArchived);

        Status = EventLifecycleStatus.Archived;
        AddDomainEvent(new TicketedEventStatusChangedDomainEvent(Id, TeamId, Slug, Status));
    }

    public void ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy policy)
    {
        EnsureActive();
        RegistrationPolicy = policy;
    }

    public void ConfigureCancellationPolicy(TicketedEventCancellationPolicy? policy)
    {
        EnsureActive();
        CancellationPolicy = policy;
    }

    public void ConfigureReconfirmPolicy(TicketedEventReconfirmPolicy? policy)
    {
        EnsureActive();
        ReconfirmPolicy = policy;
        AddDomainEvent(new TicketedEventReconfirmPolicyChangedDomainEvent(TeamId, Id, policy));
    }

    public void UpdateAdditionalDetailSchema(IReadOnlyList<AdditionalDetailField> fields)
    {
        EnsureActive();

        var schema = AdditionalDetailSchema.Create(fields);
        AdditionalDetailSchema = schema;

        AddDomainEvent(new AdditionalDetailSchemaUpdatedDomainEvent(Id, TeamId, Slug, schema));
    }

    /// <summary>
    /// Derived "is registration open" — requires a policy, the current time to fall
    /// within the window, and the event to be <see cref="EventLifecycleStatus.Active"/>.
    /// </summary>
    public bool IsRegistrationOpen(DateTimeOffset now) =>
        IsActive
        && RegistrationPolicy is not null
        && RegistrationPolicy.IsWithinWindow(now);

    private void EnsureActive()
    {
        if (!IsActive)
            throw new BusinessRuleViolationException(Errors.EventNotActive);
    }

    internal static class Errors
    {
        public static readonly Error EndBeforeStart = new(
            "ticketed_event.end_before_start",
            "Event end time must be on or after the start time.");

        public static readonly Error EventNotActive = new(
            "ticketed_event.event_not_active",
            "Operation not allowed: the ticketed event is not Active.",
            Type: ErrorType.Validation);

        public static readonly Error AlreadyCancelled = new(
            "ticketed_event.already_cancelled",
            "The ticketed event is already cancelled.");

        public static readonly Error AlreadyArchived = new(
            "ticketed_event.already_archived",
            "The ticketed event is already archived.");
    }
}
