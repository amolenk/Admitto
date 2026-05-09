using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Domain.DomainEvents;

/// <summary>
/// Raised when a <see cref="Entities.BulkEmailJob"/> has been created and is
/// awaiting fan-out. The Worker host's module-event handler turns this into a
/// one-shot Quartz trigger that runs <c>SendBulkEmailJob</c>.
/// </summary>
public sealed record BulkEmailJobRequestedDomainEvent(
    BulkEmailJobId BulkEmailJobId,
    TeamId TeamId,
    TicketedEventId TicketedEventId) : DomainEvent;
