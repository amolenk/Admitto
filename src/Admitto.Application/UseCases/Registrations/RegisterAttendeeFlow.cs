// using Amolenk.Admitto.Application.UseCases.Email.SendAttendeeEmail;
// using Amolenk.Admitto.Application.UseCases.Email.SendEmail;
// using Amolenk.Admitto.Application.UseCases.Registrations.CompleteRegistration;
// using Amolenk.Admitto.Application.UseCases.Registrations.RejectRegistration;
// using Amolenk.Admitto.Application.UseCases.TicketedEvents.ReserveTickets;
// using Amolenk.Admitto.Domain.DomainEvents;
// using Amolenk.Admitto.Domain.Utilities;
// using Amolenk.Admitto.Domain.ValueObjects;
//
// namespace Amolenk.Admitto.Application.UseCases.Registrations;
//
// /// <summary>
// /// Represents a flow that orchestrates the entire registration process for an attendee.
// /// </summary>
// public class RegisterAttendeeFlow(IMessageSender messageSender, IJobScheduler jobScheduler)
//     : IEventualDomainEventHandler<RegistrationReceivedDomainEvent>
//     , IEventualDomainEventHandler<UserVerifiedRegistrationDomainEvent>
//     , IEventualDomainEventHandler<TicketsReservedDomainEvent>
//     , IEventualDomainEventHandler<RegistrationCompletedDomainEvent>
//     , IEventualDomainEventHandler<TicketsReservationRejectedDomainEvent>
//     , IEventualDomainEventHandler<RegistrationRejectedDomainEvent>
// {
//     public async ValueTask HandleAsync(RegistrationReceivedDomainEvent domainEvent, CancellationToken cancellationToken)
//     {
//         // Anyone can start a public registration for a ticketed event. To guard against misuse, we'll first ask the
//         // user to confirm via email. During this time, we will not reserve any tickets yet.
//         if (domainEvent.RegistrationType == RegistrationType.Public)
//         {
//             var jobId = DeterministicGuid.Create($"{domainEvent.Id}:{nameof(SendEmailJobData)}");
//             var emailJobData = new SendEmailJobData(jobId, domainEvent.RegistrationId, 
//                 EmailTemplateType.ConfirmRegistration);
//
//             await jobScheduler.AddJobAsync(emailJobData, cancellationToken);
//             return;
//         }
//
//         // If the registration type is not public, we can directly reserve the tickets.
//         var command = new ReserveTicketsCommand(domainEvent.TicketedEventId, domainEvent.RegistrationId, 
//             domainEvent.RegistrationType, domainEvent.Tickets)
//         {
//             CommandId = DeterministicGuid.Create($"{domainEvent.Id}:{nameof(ReserveTicketsCommand)}")
//         };
//         
//         await messageSender.SendAsync(Message.FromCommand(command), cancellationToken);
//     }
//     
//     public ValueTask HandleAsync(UserVerifiedRegistrationDomainEvent domainEvent, CancellationToken cancellationToken)
//     {
//         // Once the user has confirmed the registration, we need to reserve the actual tickets.
//         var command = new ReserveTicketsCommand(domainEvent.TicketedEventId, domainEvent.RegistrationId, 
//             domainEvent.RegistrationType, domainEvent.Tickets)
//         {
//             CommandId = DeterministicGuid.Create($"{domainEvent.Id}:{nameof(ReserveTicketsCommand)}")
//         };
//             
//         return messageSender.SendAsync(Message.FromCommand(command), cancellationToken);
//     }
//
//     public ValueTask HandleAsync(TicketsReservedDomainEvent domainEvent, CancellationToken cancellationToken)
//     {
//         // Once the tickets have been reserved, we can complete the registration.
//         var command = new CompleteRegistrationCommand(domainEvent.RegistrationId)
//         {
//             CommandId = DeterministicGuid.Create($"{domainEvent.Id}:{nameof(CompleteRegistrationCommand)}")
//         };
//         
//         return messageSender.SendAsync(Message.FromCommand(command), cancellationToken);
//     }
//
//     public async ValueTask HandleAsync(RegistrationCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
//     {
//         // Once the registration is completed, we need to send the ticket email to the user.
//         var jobId = DeterministicGuid.Create($"{domainEvent.Id}:{nameof(SendEmailJobData)}");
//         var emailJobData = new SendEmailJobData(jobId, domainEvent.RegistrationId, EmailTemplateType.Ticket);
//
//         await jobScheduler.AddJobAsync(emailJobData, cancellationToken);
//     }
//
//     public ValueTask HandleAsync(TicketsReservationRejectedDomainEvent domainEvent, CancellationToken cancellationToken)
//     {
//         // If the tickets could not be reserved, we need to reject the registration.
//         var command = new RejectRegistrationCommand(domainEvent.RegistrationId)
//         {
//             CommandId = DeterministicGuid.Create($"{domainEvent.Id}:{nameof(RejectRegistrationCommand)}")
//         };
//
//         return messageSender.SendAsync(Message.FromCommand(command), cancellationToken);
//     }
//     
//     public async ValueTask HandleAsync(RegistrationRejectedDomainEvent domainEvent, CancellationToken cancellationToken)
//     {
//         // If the registration is rejected, we need to send a rejection email to the user.
//         var jobId = DeterministicGuid.Create($"{domainEvent.Id}:{nameof(SendEmailJobData)}");
//         var emailJobData = new SendEmailJobData(jobId, domainEvent.RegistrationId, EmailTemplateType.RegistrationRejected);
//
//         await jobScheduler.AddJobAsync(emailJobData, cancellationToken);
//     }
// }
