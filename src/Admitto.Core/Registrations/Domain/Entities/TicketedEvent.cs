using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Domain.Entities;

/// <summary>
/// Authoritative aggregate for a ticketed event in the Registrations module.
/// Owns the name/dates, the lifecycle status, and event policies as value objects.
/// </summary>
/// <remarks>
/// Policy mutators reject when the aggregate is not Active; lifecycle
/// transitions are one-way (Active → Archived).
/// </remarks>
public class TicketedEvent : Aggregate<TicketedEventId>
{
    // ReSharper disable once UnusedMember.Local — required by EF Core
    private TicketedEvent() { }

    private TicketedEvent(
        TicketedEventId id,
        TeamId teamId,
        EventName name,
        AbsoluteUrl websiteUrl,
        AbsoluteUrl baseUrl,
        Slug publicSlug,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        TimeZoneId timeZone)
        : base(id)
    {
        TeamId = teamId;
        Name = name;
        WebsiteUrl = websiteUrl;
        BaseUrl = baseUrl;
        PublicSlug = publicSlug;
        StartsAt = startsAt;
        EndsAt = endsAt;
        TimeZone = timeZone;
        Status = EventLifecycleStatus.Active;
    }

    public TeamId TeamId { get; private set; }
    public EventName Name { get; private set; }
    public AbsoluteUrl WebsiteUrl { get; private set; }
    public AbsoluteUrl BaseUrl { get; private set; }
    public Slug PublicSlug { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public TimeZoneId TimeZone { get; private set; }
    public EventLifecycleStatus Status { get; private set; }

    public TicketedEventRegistrationPolicy? RegistrationPolicy { get; private set; }
    public TicketedEventReconfirmPolicy? ReconfirmPolicy { get; private set; }
    public TicketedEventWaitlistPolicy WaitlistPolicy { get; private set; } = TicketedEventWaitlistPolicy.Default();
    public AdditionalDetailSchema AdditionalDetailSchema { get; private set; } = AdditionalDetailSchema.Empty;

    public bool IsActive => Status == EventLifecycleStatus.Active;

    public static TicketedEvent Create(
        CreationRequestId creationRequestId,
        TicketedEventId id,
        TeamId teamId,
        EventName name,
        AbsoluteUrl websiteUrl,
        AbsoluteUrl baseUrl,
        Slug publicSlug,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        TimeZoneId timeZone)
    {
        if (endsAt < startsAt)
            throw new BusinessRuleViolationException(Errors.EndBeforeStart);

        var ticketedEvent = new TicketedEvent(
            id, teamId, name, websiteUrl, baseUrl, publicSlug, startsAt, endsAt, timeZone);

        ticketedEvent.AddDomainEvent(
            new TicketedEventCreatedDomainEvent(
                creationRequestId,
                teamId,
                id,
                ticketedEvent.Version,
                name,
                websiteUrl,
                publicSlug,
                timeZone));

        return ticketedEvent;
    }

    public static TicketedEvent Create(
        CreationRequestId creationRequestId,
        TicketedEventId id,
        TeamId teamId,
        EventName name,
        AbsoluteUrl websiteUrl,
        AbsoluteUrl baseUrl,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        TimeZoneId timeZone) =>
        Create(
            creationRequestId,
            id,
            teamId,
            name,
            websiteUrl,
            baseUrl,
            Slug.From(name.Value),
            startsAt,
            endsAt,
            timeZone);

    public void ConfigureWaitlistPolicy(TimeOnly quietHoursStart, TimeOnly quietHoursEnd)
    {
        EnsureActive();

        WaitlistPolicy = TicketedEventWaitlistPolicy.Create(quietHoursStart, quietHoursEnd);
    }

    public void UpdateDetails(
        EventName name,
        AbsoluteUrl websiteUrl,
        AbsoluteUrl baseUrl,
        Slug publicSlug,
        TimeZoneId timeZone,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt)
    {
        EnsureActive();

        if (endsAt < startsAt)
            throw new BusinessRuleViolationException(Errors.EndBeforeStart);

        if (RegistrationPolicy is not null && RegistrationPolicy.ClosesAt > startsAt)
            throw new BusinessRuleViolationException(Errors.RegistrationWindowClosesAfterEventStart);

        if (ReconfirmPolicy is not null && ReconfirmPolicy.ClosesAt >= startsAt)
            throw new BusinessRuleViolationException(Errors.ReconfirmWindowClosesAfterEventStart);

        Name = name;
        WebsiteUrl = websiteUrl;
        BaseUrl = baseUrl;
        PublicSlug = publicSlug;
        TimeZone = timeZone;
        StartsAt = startsAt;
        EndsAt = endsAt;

        AddDomainEvent(new TicketedEventDetailsChangedDomainEvent(
            TeamId,
            Id,
            Version,
            Name,
            WebsiteUrl,
            PublicSlug,
            TimeZone));
    }

    public void UpdateDetails(
        EventName name,
        AbsoluteUrl websiteUrl,
        AbsoluteUrl baseUrl,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt) =>
        UpdateDetails(name, websiteUrl, baseUrl, PublicSlug, TimeZone, startsAt, endsAt);

    public void Archive()
    {
        EnsureActive();

        Status = EventLifecycleStatus.Archived;
        AddDomainEvent(new TicketedEventStatusChangedDomainEvent(Id, TeamId, Version, Status));
    }

    public void ConfigureRegistrationPolicy(TicketedEventRegistrationPolicy? policy)
    {
        EnsureActive();

        if (policy is not null && policy.ClosesAt > StartsAt)
            throw new BusinessRuleViolationException(Errors.RegistrationWindowClosesAfterEventStart);

        RegistrationPolicy = policy;
    }

    public void ConfigureReconfirmPolicy(TicketedEventReconfirmPolicy? policy)
    {
        EnsureActive();

        if (policy is not null && policy.ClosesAt >= StartsAt)
            throw new BusinessRuleViolationException(Errors.ReconfirmWindowClosesAfterEventStart);

        ReconfirmPolicy = policy;
        AddDomainEvent(new TicketedEventReconfirmPolicyChangedDomainEvent(TeamId, Id, Version, policy));
    }

    public void UpdateAdditionalDetailSchema(IReadOnlyList<AdditionalDetailField> fields)
    {
        EnsureActive();

        var schema = AdditionalDetailSchema.Create(fields);
        AdditionalDetailSchema = schema;

        AddDomainEvent(new AdditionalDetailSchemaUpdatedDomainEvent(Id, TeamId, schema));
    }

    /// <summary>
    /// Derived "is registration open" — requires a policy, the current time to fall
    /// within the window, and the event to be <see cref="EventLifecycleStatus.Active"/>.
    /// </summary>
    public bool IsRegistrationOpen(DateTimeOffset now) =>
        IsActive
        && RegistrationPolicy is not null
        && RegistrationPolicy.IsWithinWindow(now);

    /// <summary>
    /// Enforces that the registration window is currently open.
    /// Throws <see cref="BusinessRuleViolationException"/> if the policy is absent,
    /// the window has not yet started, or the window has already closed.
    /// </summary>
    public void EnsureRegistrationOpen(DateTimeOffset now)
    {
        if (RegistrationPolicy is null || now < RegistrationPolicy.OpensAt)
            throw new BusinessRuleViolationException(Errors.RegistrationNotOpen);

        if (now >= RegistrationPolicy.ClosesAt)
            throw new BusinessRuleViolationException(Errors.RegistrationClosed);
    }

    /// <summary>
    /// Enforces that the supplied email address matches any domain restriction
    /// configured on the registration policy.
    /// </summary>
    public void EnsureEmailDomainAllowed(EmailAddress email)
    {
        if (RegistrationPolicy is not null && !RegistrationPolicy.IsEmailDomainAllowed(email.Value))
            throw new BusinessRuleViolationException(Errors.EmailDomainNotAllowed);
    }

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

        public static readonly Error RegistrationWindowClosesAfterEventStart = new(
            "ticketed_event.registration_window_closes_after_event_start",
            "Registration window must close on or before the event start date.");

        public static readonly Error ReconfirmWindowClosesAfterEventStart = new(
            "ticketed_event.reconfirm_window_closes_after_event_start",
            "Reconfirmation window must close before the event start date.");

        public static readonly Error RegistrationNotOpen = new(
            "registration.not_open",
            "Registration is not open for this event.",
            Type: ErrorType.Validation);

        public static readonly Error RegistrationClosed = new(
            "registration.closed",
            "Registration for this event has closed.",
            Type: ErrorType.Validation);

        public static readonly Error EmailDomainNotAllowed = new(
            "registration.email_domain_not_allowed",
            "Your email domain is not allowed for this event.",
            Type: ErrorType.Validation);
    }
}
