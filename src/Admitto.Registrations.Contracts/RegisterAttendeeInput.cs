namespace Amolenk.Admitto.Registrations.Contracts;

public record RegisterAttendeeInput(
    Guid TeamId,
    Guid EventId,
    string Email,
    string[] TicketTypeSlugs,
    TicketGrantModeDto GrantMode);