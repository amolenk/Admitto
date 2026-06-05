using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

/// <summary>
/// Raised by the <c>TicketedEvent</c> aggregate when its lifecycle status transitions
/// (Active→Cancelled, Active→Archived, Cancelled→Archived). Consumed within the
/// Registrations module to project <c>EventStatus</c> onto the event's <c>TicketCatalog</c>
/// in the same unit of work as the lifecycle change.
/// </summary>
public record TicketedEventStatusChangedDomainEvent(
    TicketedEventId TicketedEventId,
    TeamId TeamId,
    EventLifecycleStatus NewStatus) : DomainEvent;
