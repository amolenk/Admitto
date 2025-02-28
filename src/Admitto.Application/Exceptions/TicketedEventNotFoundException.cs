namespace Amolenk.Admitto.Application.Exceptions;

public class TicketedEventNotFoundException(Guid ticketedEventId, Exception? innerException = null)
    : Exception($"TicketedEvent {ticketedEventId} not found.", innerException);