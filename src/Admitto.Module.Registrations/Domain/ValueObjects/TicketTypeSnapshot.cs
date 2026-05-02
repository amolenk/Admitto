namespace Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

/// <summary>
/// Snapshot of a ticket type at the time of registration, keyed by slug.
/// </summary>
public sealed record TicketTypeSnapshot(string Slug, string Name, string[] TimeSlots);