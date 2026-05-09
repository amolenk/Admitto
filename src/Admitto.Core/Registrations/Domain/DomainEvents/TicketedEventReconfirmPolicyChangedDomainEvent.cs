using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

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
