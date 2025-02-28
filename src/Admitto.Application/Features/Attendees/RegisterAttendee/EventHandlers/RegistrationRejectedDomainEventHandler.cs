using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee.EventHandlers;

public class RegistrationRejectedDomainEventHandler : IDomainEventHandler<RegistrationRejectedDomainEvent>
{
    public ValueTask HandleAsync(RegistrationRejectedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        Console.WriteLine("Registration rejected!");
        return ValueTask.CompletedTask;
    }
}