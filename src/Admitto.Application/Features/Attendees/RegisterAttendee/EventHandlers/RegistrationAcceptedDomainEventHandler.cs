using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee.EventHandlers;

public class RegistrationAcceptedDomainEventHandler : IDomainEventHandler<RegistrationAcceptedDomainEvent>
{
    public ValueTask HandleAsync(RegistrationAcceptedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        Console.WriteLine("Registration accepted!");
        return ValueTask.CompletedTask;
    }
}