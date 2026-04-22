using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;

/// <summary>
/// Raised by the <c>TicketedEvent</c> aggregate when its additional-detail schema is
/// atomically replaced via <c>UpdateAdditionalDetailSchema</c>.
/// </summary>
public record AdditionalDetailSchemaUpdatedDomainEvent(
    TicketedEventId TicketedEventId,
    TeamId TeamId,
    Slug Slug,
    AdditionalDetailSchema Schema) : DomainEvent;
