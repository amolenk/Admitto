using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Domain.DomainEvents;

/// <summary>
/// Raised when a ticketed event is archived. Consumed by the Registrations module
/// to sync the event lifecycle status.
/// </summary>
public sealed record TicketedEventArchivedDomainEvent(TicketedEventId TicketedEventId) : DomainEvent;
