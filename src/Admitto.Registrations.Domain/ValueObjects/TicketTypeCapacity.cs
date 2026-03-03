// namespace Amolenk.Admitto.Registrations.Domain.ValueObjects;
//
// /// <summary>
// /// Represents the capacity information for a specific ticket type within a ticketed event.
// /// Used capacity is allowed to be greater than max capacity. This is useful for scenarios where
// /// we don't want to refill the capacity when tickets are cancelled.
// /// </summary>
// public sealed record TicketTypeCapacity
// {
//     //If you have modes like:
//     // •	Strict (no oversell; fail if full)
//     // •	AllowOversell (increment even if full)
//     // •	DoNotRefillOnCancel etc.
//     
//     
//     
//     public TicketedEventId EventId { get; }
//     public TicketTypeId TicketTypeId { get; }
//     public int MaxCapacity { get; }
//     public int UsedCapacity { get; private set; }
//
//     private TicketTypeCapacity(
//         TicketedEventId eventId,
//         TicketTypeId ticketTypeId,
//         int maxCapacity,
//         int usedCapacity)
//     {
//         ArgumentOutOfRangeException.ThrowIfNegative(maxCapacity);
//         ArgumentOutOfRangeException.ThrowIfNegative(usedCapacity);
//
//         EventId = eventId;
//         TicketTypeId = ticketTypeId;
//         MaxCapacity = maxCapacity;
//         UsedCapacity = usedCapacity;
//     }
//
//     public static TicketTypeCapacity Create(
//         TicketedEventId eventId,
//         TicketTypeId ticketTypeId,
//         int maxCapacity,
//         int usedCapacity)
//         => new(eventId, ticketTypeId, maxCapacity, usedCapacity);
//
//     public int RemainingCapacity => Math.Max(0, MaxCapacity - UsedCapacity);
//
//     public void Claim(CapacityEnforcementMode mode)
//     {
//         // if (!bypassMaxCapacity && UsedCapacity >= MaxCapacity)
//         // {
//         //     throw new InvalidOperationException("Cannot claim capacity: max capacity reached.");
//         // }
//
//         UsedCapacity++;
//     }
// }