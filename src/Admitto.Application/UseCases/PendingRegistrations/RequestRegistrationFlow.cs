using Amolenk.Admitto.Application.Jobs.SendEmail;
using Amolenk.Admitto.Application.UseCases.Email.SendEmail;
using Amolenk.Admitto.Application.UseCases.Registrations.CompleteRegistration;
using Amolenk.Admitto.Application.UseCases.Registrations.RejectRegistration;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.PendingRegistrations;

/// <summary>
/// Represents a flow that orchestrates the entire registration process for an attendee.
/// </summary>
public class RequestRegistrationFlow(IMessageSender messageSender, IJobScheduler jobScheduler)
    : IEventualDomainEventHandler<PendingRegistrationReceivedDomainEvent>
        , IEventualDomainEventHandler<PendingRegistrationVerifiedDomainEvent>
        , IEventualDomainEventHandler<TicketsReservedDomainEvent>
        , IEventualDomainEventHandler<TicketsReservationRejectedDomainEvent>
{
    /// <summary>
    /// Anyone can start a public registration for a ticketed event. To guard against misuse, we'll first ask the
    /// user to confirm via email. During this time, we will not reserve any tickets yet.
    /// </summary>
    public async ValueTask HandleAsync(
        PendingRegistrationReceivedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var jobId = DeterministicGuid.Create($"{domainEvent.Id}:{nameof(SendEmailJobData)}");
        var emailJobData = new SendEmailJobData(
            jobId,
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            EmailType.VerifyRegistration,
            domainEvent.RegistrationRequestId);

        await jobScheduler.AddJobAsync(emailJobData, cancellationToken);
    }

    public ValueTask HandleAsync(
        PendingRegistrationVerifiedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();

        // Once the user has confirmed the registration, we need to reserve the actual tickets.
        // var command = new ReserveTicketsCommand(
        //     domainEvent.TicketedEventId,
        //     domainEvent.RegistrationRequestId,
        //     domainEvent.Tickets)
        // {
        //     CommandId = DeterministicGuid.Create($"{domainEvent.Id}:{nameof(ReserveTicketsCommand)}")
        // };
        //
        // return messageSender.SendAsync(Message.FromCommand(command), cancellationToken);
    }

    public ValueTask HandleAsync(TicketsReservedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Once the tickets have been reserved, we can complete the registration.
        var command = new CompleteRegistrationCommand(domainEvent.RegistrationId) // TODO CompleteRegistrationRequest
        {
            CommandId = DeterministicGuid.Create($"{domainEvent.Id}:{nameof(CompleteRegistrationCommand)}")
        };

        return messageSender.SendAsync(Message.FromCommand(command), cancellationToken);
    }

    public ValueTask HandleAsync(TicketsReservationRejectedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // If the tickets could not be reserved, we need to reject the registration.
        var command = new RejectRegistrationCommand(domainEvent.RegistrationId)
        {
            CommandId = DeterministicGuid.Create($"{domainEvent.Id}:{nameof(RejectRegistrationCommand)}")
        };

        return messageSender.SendAsync(Message.FromCommand(command), cancellationToken);
    }
}