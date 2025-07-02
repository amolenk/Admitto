using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.ReadModel.Projections;

public class PendingRegistrationsProjectionHandler(IReadModelContext context) 
    : IEventualDomainEventHandler<TicketsReservedDomainEvent>
{
    public ValueTask HandleAsync(TicketsReservedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
          // TODO Create a view model of all pending registrations
          
          // domainEvent.RegistrationId
          // domainEvent.ConfirmationCode
          // domainEvent.ExpirationTime
          
          return ValueTask.CompletedTask;
    }
}