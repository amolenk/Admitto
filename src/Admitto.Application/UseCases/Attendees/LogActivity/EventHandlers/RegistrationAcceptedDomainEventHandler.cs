using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.UseCases.Attendees.LogActivity.EventHandlers;

/// <summary>
/// Write to the activity log when a registration is confirmed.
/// The command ID is generated deterministically based on the domain event ID to ensure idempotency.
/// </summary>
public class RegistrationAcceptedDomainEventHandler(LogActivityHandler handler)
    : IDomainEventHandler<RegistrationAcceptedDomainEvent>
{
    public ValueTask HandleAsync(RegistrationAcceptedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var command = new LogActivityCommand(domainEvent.AttendeeId, "Registration accepted", domainEvent.OccurredOn)
        {
            CommandId = DeterministicGuidGenerator.Generate($"{domainEvent.DomainEventId}-LogActivity")
        };
        
        return handler.HandleAsync(command, cancellationToken);
    }
}