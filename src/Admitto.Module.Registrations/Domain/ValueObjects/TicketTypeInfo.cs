namespace Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

/// <summary>
/// Snapshot of ticket type information used for coupon validation at creation time.
/// </summary>
public sealed record TicketTypeInfo(string Slug, bool IsCancelled);
