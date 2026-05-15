using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

/// <summary>
/// Snapshot of a ticket type at the time of registration, keyed by server-generated ID.
/// </summary>
public sealed record TicketTypeSnapshot(TicketTypeId Id, TicketTypeName Name, TimeSlot[] TimeSlots);