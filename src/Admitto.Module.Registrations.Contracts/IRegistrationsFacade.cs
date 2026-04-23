namespace Amolenk.Admitto.Module.Registrations.Contracts;

public interface IRegistrationsFacade
{
    ValueTask<TicketedEventEmailContextDto> GetTicketedEventEmailContextAsync(
        Guid ticketedEventId,
        CancellationToken cancellationToken = default);
}
