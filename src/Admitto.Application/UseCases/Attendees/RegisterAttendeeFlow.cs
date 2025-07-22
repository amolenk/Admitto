using Amolenk.Admitto.Application.Jobs.SendEmail;
using Amolenk.Admitto.Application.UseCases.Attendees.CompleteRegistration;
using Amolenk.Admitto.Application.UseCases.Attendees.FailRegistration;
using Amolenk.Admitto.Application.UseCases.TicketedEvents.ReserveTickets;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.Utilities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees;

/// <summary>
/// Represents a flow that orchestrates the entire registration process for an attendee.
/// </summary>
public class RegisterAttendeeFlow(IMessageSender messageSender, IJobScheduler jobScheduler)
    : IEventualDomainEventHandler<AttendeeSignedUpDomainEvent>
        , IEventualDomainEventHandler<AttendeeVerifiedDomainEvent>
        , IEventualDomainEventHandler<AttendeeInvitedDomainEvent>
        , IEventualDomainEventHandler<TicketsReservedDomainEvent>
        , IEventualDomainEventHandler<TicketsUnavailableDomainEvent>
        , IEventualDomainEventHandler<RegistrationCompletedDomainEvent>
        , IEventualDomainEventHandler<RegistrationFailedDomainEvent>
{
    /// <summary>
    /// Anyone can start a public registration for a ticketed event. To guard against misuse, we'll first ask the
    /// attendee to confirm via email. During this time, we will not reserve any tickets yet.
    /// </summary>
    public async ValueTask HandleAsync(AttendeeSignedUpDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var jobId = DeterministicGuid.Create($"{domainEvent.DomainEventId}:{nameof(SendEmailJobData)}");
        var emailJobData = new SendEmailJobData(
            jobId,
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.AttendeeId,
            EmailType.VerifyEmail);

        await jobScheduler.AddJobAsync(emailJobData, cancellationToken);
    }

    /// <summary>
    /// Once the attendee has been verified, we need to reserve the actual tickets.
    /// Invited attendees are not required to verify their email, and we ignore ticket availability in that case.
    /// </summary>
    public ValueTask HandleAsync(AttendeeVerifiedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var command = new ReserveTicketsCommand(
            domainEvent.TicketedEventId,
            domainEvent.AttendeeId,
            domainEvent.Tickets)
        {
            CommandId = DeterministicGuid.Create($"{domainEvent.DomainEventId}:{nameof(ReserveTicketsCommand)}")
        };

        return messageSender.SendAsync(Message.FromCommand(command), cancellationToken);
    }
    
    /// <summary>
    /// Invited attendees are not required to verify their email, and we ignore ticket availability in that case.
    /// </summary>
    public ValueTask HandleAsync(AttendeeInvitedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var command = new ReserveTicketsCommand(
            domainEvent.TicketedEventId,
            domainEvent.AttendeeId,
            domainEvent.Tickets,
            IgnoreAvailability: true)
        {
            CommandId = DeterministicGuid.Create($"{domainEvent.DomainEventId}:{nameof(ReserveTicketsCommand)}")
        };

        return messageSender.SendAsync(Message.FromCommand(command), cancellationToken);
    }

    /// <summary>
    /// Once the tickets have been reserved, we can complete the registration.
    /// </summary>
    public ValueTask HandleAsync(TicketsReservedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var command = new CompleteRegistrationCommand(domainEvent.AttendeeId)
        {
            CommandId = DeterministicGuid.Create($"{domainEvent.DomainEventId}:{nameof(CompleteRegistrationCommand)}")
        };

        return messageSender.SendAsync(Message.FromCommand(command), cancellationToken);
    }

    /// <summary>
    /// If the tickets could not be reserved, we need to reject the registration.
    /// </summary>
    public ValueTask HandleAsync(TicketsUnavailableDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var command = new FailRegistrationCommand(domainEvent.AttendeeId)
        {
            CommandId = DeterministicGuid.Create($"{domainEvent.DomainEventId}:{nameof(FailRegistrationCommand)}")
        };

        return messageSender.SendAsync(Message.FromCommand(command), cancellationToken);
    }

    /// <summary>
    /// If the registration was successful, send the tickets email.
    /// </summary>
    public async ValueTask HandleAsync(RegistrationCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var jobId = DeterministicGuid.Create($"{domainEvent.DomainEventId}:{nameof(SendEmailJobData)}");
        var emailJobData = new SendEmailJobData(
            jobId,
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.AttendeeId,
            EmailType.Ticket);

        await jobScheduler.AddJobAsync(emailJobData, cancellationToken);
    }
    
    /// <summary>
    /// If the registration failed, send a decline email.
    /// </summary>
    public async ValueTask HandleAsync(RegistrationFailedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var jobId = DeterministicGuid.Create($"{domainEvent.DomainEventId}:{nameof(SendEmailJobData)}");
        var emailJobData = new SendEmailJobData(
            jobId,
            domainEvent.TeamId,
            domainEvent.TicketedEventId,
            domainEvent.AttendeeId,
            EmailType.RegistrationFailed);

        await jobScheduler.AddJobAsync(emailJobData, cancellationToken);
    }
}