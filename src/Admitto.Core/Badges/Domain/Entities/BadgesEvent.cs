using Amolenk.Admitto.Core.Shared.Kernel.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Badges.Domain.Entities;

/// <summary>
/// Projection of a <see cref="TicketedEvent"/> lifecycle into the Badges module.
/// Created when the event is created; transitions to Archived when the event is archived.
/// Guards badge-type mutation commands.
/// </summary>
public sealed class BadgesEvent : Entity<TicketedEventId>
{
    private BadgesEvent() { }

    private BadgesEvent(TicketedEventId eventId, TeamId teamId, BadgeEventStatus status)
        : base(eventId)
    {
        TeamId = teamId;
        Status = status;
    }

    public TeamId TeamId { get; private set; }
    public BadgeEventStatus Status { get; private set; }

    public static BadgesEvent Create(TicketedEventId eventId, TeamId teamId)
        => new(eventId, teamId, BadgeEventStatus.Active);

    public void MarkArchived()
    {
        Status = BadgeEventStatus.Archived;
    }

    public void EnsureEventActive()
    {
        if (Status != BadgeEventStatus.Active)
            throw new BusinessRuleViolationException(Errors.EventNotActive);
    }

    internal static class Errors
    {
        public static readonly Error EventNotActive = new(
            "badges_event.event_not_active",
            "Badge types cannot be modified because the event is not active.",
            Type: ErrorType.Validation);
    }
}
