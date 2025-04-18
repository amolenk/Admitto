namespace Amolenk.Admitto.Application.Common;

public static class Error
{
    public static string TeamNotFound(Guid teamId) => $"Team {teamId} not found.";
    public static string TicketedEventNotFound(Guid ticketedEventId) => $"Event {ticketedEventId} not found.";
    public const string InsufficientCapacity = "Insufficient capacity for event.";

    public static string AttendeeRegistrationNotFound(Guid registrationId) => $"Registration {registrationId} not found.";
}