using Amolenk.Admitto.Application.Jobs.SendEmail;
using Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;
using Amolenk.Admitto.Application.UseCases.Registrations.CompleteRegistration;
using Amolenk.Admitto.Application.UseCases.TicketedEvents.ReserveTickets;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Registrations;

/// <summary>
/// Represents a saga that orchestrates the entire registration process for an attendee.
/// </summary>
public class RegisterAttendeeSaga(ICommandSender commandSender, IJobScheduler jobScheduler)
    : IEventualDomainEventHandler<RegistrationReceivedDomainEvent>
    , IEventualDomainEventHandler<UserConfirmedRegistrationDomainEvent>
    , IEventualDomainEventHandler<TicketsReservedDomainEvent>
    , IEventualDomainEventHandler<RegistrationCompletedDomainEvent>
    , IEventualDomainEventHandler<TicketsReservationRejectedDomainEvent>
    , IEventualDomainEventHandler<RegistrationRejectedDomainEvent>
{
    public async ValueTask HandleAsync(RegistrationReceivedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Anyone can start a new registration for a ticketed event. To guard against misuse, we'll first ask the
        // user to confirm via email. During this time, we will not reserve any tickets yet.
        var jobId = DeterministicGuidGenerator.Generate($"{domainEvent.Id}:{nameof(SendEmailJobData)}");
        var emailJobData = new SendEmailJobData(jobId, domainEvent.RegistrationId, EmailTemplateId.ConfirmRegistration);

        await jobScheduler.AddJobAsync(emailJobData, cancellationToken);
    }
    
    public ValueTask HandleAsync(UserConfirmedRegistrationDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Once the user has confirmed the registration, we need to reserve the actual tickets.
        var command = new ReserveTicketsCommand(domainEvent.TicketedEventId, domainEvent.RegistrationId, 
            domainEvent.Tickets)
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

    public async ValueTask HandleAsync(RegistrationCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Once the registration is completed, we need to send the ticket email to the user.
        var jobId = DeterministicGuidGenerator.Generate($"{domainEvent.Id}:{nameof(SendEmailJobData)}");
        var emailJobData = new SendEmailJobData(jobId, domainEvent.RegistrationId, EmailTemplateId.Ticket);

        await jobScheduler.AddJobAsync(emailJobData, cancellationToken);
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
    
    public async ValueTask HandleAsync(RegistrationRejectedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // If the registration is rejected, we need to send a rejection email to the user.
        var jobId = DeterministicGuidGenerator.Generate($"{domainEvent.Id}:{nameof(SendEmailJobData)}");
        var emailJobData = new SendEmailJobData(jobId, domainEvent.RegistrationId, EmailTemplateId.RegistrationRejected);

        await jobScheduler.AddJobAsync(emailJobData, cancellationToken);
    }
}
