namespace Amolenk.Admitto.Domain.Exceptions;

public class TicketsUnavailableException : DomainException
{
    public TicketsUnavailableException()
    : base("Tickets are unavailable.")
    {
    }
}