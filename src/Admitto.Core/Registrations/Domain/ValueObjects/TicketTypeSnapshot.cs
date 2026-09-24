namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

/// <summary>
/// Snapshot of a ticket type at the time of registration, keyed by server-generated ID.
/// <see cref="Mode"/> records which capacity pool the claim consumed so a later
/// <see cref="Entities.TicketCatalog.Release"/> can credit the correct counter back.
/// </summary>
public sealed record TicketTypeSnapshot(
    TicketTypeId Id,
    TicketTypeName Name,
    TimeSlot[] TimeSlots,
    ClaimMode Mode = ClaimMode.Public);