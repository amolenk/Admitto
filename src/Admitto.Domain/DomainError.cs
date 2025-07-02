using Amolenk.Admitto.Domain.Exceptions;

namespace Amolenk.Admitto.Domain;

public static class DomainError
{
    public static class AttendeeRegistration
    {
        public static DomainException TicketTypeAlreadyExists()
        {
            return new DomainException(ErrorMessage.AttendeeRegistration.Tickets.TicketType.AlreadyExists);
        }
    }

    public static class TicketedEvent
    {
        public static DomainException TicketTypeAlreadyExists()
        {
            return new DomainException(ErrorMessage.TicketedEvent.TicketType.AlreadyExists);
        }
    }

    public static class TicketType
    {
        public static DomainException QuantityMustBeGreaterThanZero()
        {
            return new DomainException(ErrorMessage.Ticket.Quantity.MustBeGreaterThanZero);
        }
    }
}