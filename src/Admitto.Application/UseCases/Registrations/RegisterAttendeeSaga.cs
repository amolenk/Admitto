using Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;
using Amolenk.Admitto.Application.UseCases.Attendees.RequestConfirmation;
using Amolenk.Admitto.Application.UseCases.Registrations.CompleteRegistration;
using Amolenk.Admitto.Application.UseCases.TicketedEvents.ReserveTickets;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;

namespace Amolenk.Admitto.Application.UseCases.Registrations;

/// <summary>
/// Represents a saga that orchestrates the entire registration process for an attendee.
/// </summary>
public class RegisterAttendeeSaga(ICommandSender commandSender)
    : IEventualDomainEventHandler<RegistrationReceivedDomainEvent>
    , IEventualDomainEventHandler<UserConfirmedRegistrationDomainEvent>
    , IEventualDomainEventHandler<TicketsReservedDomainEvent>
    , IEventualDomainEventHandler<RegistrationCompletedDomainEvent>
    , IEventualDomainEventHandler<TicketsReservationRejectedDomainEvent>
    , IEventualDomainEventHandler<RegistrationRejectedDomainEvent>
{
    public ValueTask HandleAsync(RegistrationReceivedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Anyone can start a new registration for a ticketed event. To guard against misuse, we'll first ask the
        // user to confirm via email. During this time, we will not reserve any tickets yet.
        var command = new RequestUserConfirmationCommand(domainEvent.TicketedEventId, domainEvent.RegistrationId)
        {
            Id = DeterministicGuidGenerator.Generate($"{domainEvent.Id}:{nameof(RequestUserConfirmationCommand)}")
        };

        return commandSender.SendAsync(command);
    }
    
    public ValueTask HandleAsync(UserConfirmedRegistrationDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Once the user has confirmed the registration, we need to reserve the actual tickets.
        var command = new ReserveTicketsCommand(domainEvent.TicketedEventId, domainEvent.RegistrationId, domainEvent.Tickets)
        {
            Id = DeterministicGuidGenerator.Generate($"{domainEvent.Id}:{nameof(ReserveTicketsCommand)}")
        };
            
        return commandSender.SendAsync(command);
    }

    public ValueTask HandleAsync(TicketsReservedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Once the tickets have been reserved, we can complete the registration.
        var command = new CompleteRegistrationCommand(domainEvent.RegistrationId)
        {
            Id = DeterministicGuidGenerator.Generate($"{domainEvent.Id}:{nameof(CompleteRegistrationCommand)}")
        };
        
        return commandSender.SendAsync(command);
    }

    public ValueTask HandleAsync(RegistrationCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Once the registration is completed, we need to send the ticket email to the user.
        var command = new SendTicketEmailCommand(domainEvent.Id)
        {
            Id = DeterministicGuidGenerator.Generate($"{domainEvent.Id}:{nameof(SendTicketEmailCommand)}")
        };

        return commandSender.SendAsync(command);
    }

    public ValueTask HandleAsync(TicketsReservationRejectedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // If the tickets could not be reserved, we need to reject the registration.
        var command = new RejectRegistrationCommand
        {
            Id = DeterministicGuidGenerator.Generate($"{domainEvent.Id}:{nameof(RejectRegistrationCommand)}")
        };

        return commandSender.SendAsync(command);
    }
    
    
    public ValueTask HandleAsync(RegistrationRejectedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // If the registration is rejected, we need to send a rejection email to the user.
        var command = new SendRejectionEmailCommand(domainEvent.RegistrationId)
        {
            Id = DeterministicGuidGenerator.Generate($"{domainEvent.Id}:{nameof(SendRejectionEmailCommand)}")
        };

        return commandSender.SendAsync(command);
    }
}
