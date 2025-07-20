using Amolenk.Admitto.Domain.Exceptions;

namespace Amolenk.Admitto.Domain;

public static class DomainError
{
    public static class AttendeeRegistration
    {
        public static BusinessRuleException TicketTypeAlreadyExists()
        {
            return new BusinessRuleException(ErrorMessage.AttendeeRegistration.Tickets.TicketType.AlreadyExists);
        }
    }

    public static class TicketedEvent
    {
        public static BusinessRuleException TicketTypeAlreadyExists()
        {
            return new BusinessRuleException(ErrorMessage.TicketedEvent.TicketType.AlreadyExists);
        }
    }

    public static class TicketType
    {
        public static BusinessRuleException QuantityMustBeGreaterThanZero()
        {
            return new BusinessRuleException(ErrorMessage.Ticket.Quantity.MustBeGreaterThanZero);
        }
    }
}