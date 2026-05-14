using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

/// <summary>
/// Snapshot of a ticket type at the time of registration, keyed by slug.
/// </summary>
public sealed record TicketTypeSnapshot(Slug Slug, TicketTypeName Name, Slug[] TimeSlots);