using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;

/// <summary>
/// Raised by the <c>TicketedEvent</c> aggregate when its additional-detail schema is
/// atomically replaced via <c>UpdateAdditionalDetailSchema</c>.
/// </summary>
public record AdditionalDetailSchemaUpdatedDomainEvent(
    TicketedEventId TicketedEventId,
    TeamId TeamId,
    AdditionalDetailSchema Schema) : DomainEvent;
