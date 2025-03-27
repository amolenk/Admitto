namespace Amolenk.Admitto.Application.Common.Exceptions;

public class TicketedEventNotFoundException(Guid teamId, Guid ticketedEventId, Exception? innerException = null)
    : Exception($"Event {ticketedEventId} not found.", innerException);