using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

/// <summary>
/// Raised by the <c>TicketedEvent</c> aggregate when its reconfirm policy is
/// set, updated, or cleared. Mapped by <c>RegistrationsMessagePolicy</c> to a
/// <c>TicketedEventReconfirmPolicyChangedIntegrationEvent</c> so the Email
/// module can update its hourly-evaluation projection.
/// </summary>
public sealed record TicketedEventReconfirmPolicyChangedDomainEvent(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    uint TicketedEventVersion,
    TicketedEventReconfirmPolicy? Policy) : DomainEvent;
