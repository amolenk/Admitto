using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;

/// <summary>
/// Raised by the <c>TicketedEvent</c> aggregate when its reconfirm policy is
/// set, updated, or cleared. Mapped by <c>RegistrationsMessagePolicy</c> to a
/// <c>TicketedEventReconfirmPolicyChangedIntegrationEvent</c> so the Email
/// module's reconfirm scheduler can (re)register or remove the per-event
/// Quartz trigger.
/// </summary>
public sealed record TicketedEventReconfirmPolicyChangedDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    TicketedEventReconfirmPolicy? Policy) : DomainEvent;
