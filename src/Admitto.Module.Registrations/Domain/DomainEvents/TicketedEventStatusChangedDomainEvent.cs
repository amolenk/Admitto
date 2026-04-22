using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;

/// <summary>
/// Raised by the <c>TicketedEvent</c> aggregate when its lifecycle status transitions
/// (Active→Cancelled, Active→Archived, Cancelled→Archived). Consumed within the
/// Registrations module to project <c>EventStatus</c> onto the event's <c>TicketCatalog</c>
/// in the same unit of work as the lifecycle change.
/// </summary>
public record TicketedEventStatusChangedDomainEvent(
    TicketedEventId TicketedEventId,
    TeamId TeamId,
    Slug Slug,
    EventLifecycleStatus NewStatus) : DomainEvent;
