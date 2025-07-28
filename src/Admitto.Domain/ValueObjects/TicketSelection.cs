namespace Amolenk.Admitto.Domain.ValueObjects;

/// <summary>
/// Represents the user’s intention to claim a particular ticket type.
/// </summary>
public record TicketSelection(string TicketTypeSlug, int Quantity);
