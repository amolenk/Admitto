namespace Amolenk.Admitto.Domain.ValueObjects;

/// <summary>
/// Represents a fully qualified ticketed event ID, which consists of a team ID and a ticketed event ID.
/// </summary>
public record FullyQualifiedTicketedEventId(TeamId TeamId, TicketedEventId TicketedEventId);
