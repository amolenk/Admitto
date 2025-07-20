// using Amolenk.Admitto.Application.Jobs.SendEmail;
// using Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;
// using Amolenk.Admitto.Application.UseCases.Registrations.CompleteRegistration;
// using Amolenk.Admitto.Application.UseCases.Registrations.RejectRegistration;
// using Amolenk.Admitto.Application.UseCases.TicketedEvents.ReserveTickets;
// using Amolenk.Admitto.Domain.DomainEvents;
// using Amolenk.Admitto.Domain.Utilities;
// using Amolenk.Admitto.Domain.ValueObjects;
//
// namespace Amolenk.Admitto.Application.UseCases.Attendees;
//
// /// <summary>
// /// Represents a flow that orchestrates the entire registration process for an attendee.
// /// </summary>
// public class RegisterAttendeeFlow(IMessageSender messageSender, IJobScheduler jobScheduler)
//     : IEventualDomainEventHandler<RegistrationRequestAcceptedDomainEvent>
//         , IEventualDomainEventHandler<AttendeeRegisteredDomainEvent>
//         , IEventualDomainEventHandler<RegistrationRequestRejectedDomainEvent>
// {
//     /// <summary>
//     /// If the registration request was accepted, register the attendee.
//     /// </summary>
//     public ValueTask HandleAsync(
//         RegistrationRequestAcceptedDomainEvent domainEvent,
//         CancellationToken cancellationToken)
//     {
//         // Once the tickets have been reserved, we can complete the registration.
//         var command = new RegisterAttendeeCommand(domainEvent.RegistrationId)
//         {
//             CommandId = DeterministicGuid.Create($"{domainEvent.Id}:{nameof(CompleteRegistrationCommand)}")
//         };
//
//         return messageSender.SendAsync(Message.FromCommand(command), cancellationToken);
//     }
//
//     /// <summary>
//     /// If the registration was rejected, send a decline email.
//     /// </summary>
//     public async ValueTask HandleAsync(
//         RegistrationRequestRejectedDomainEvent domainEvent,
//         CancellationToken cancellationToken)
//     {
//         var jobId = DeterministicGuid.Create($"{domainEvent.Id}:{nameof(SendEmailJobData)}");
//         var emailJobData = new SendEmailJobData(
//             jobId,
//             domainEvent.RegistrationRequestId,
//             EmailTemplateId.RegistrationRejected);
//
//         await jobScheduler.AddJobAsync(emailJobData, cancellationToken);
//     }
//
//     /// <summary>
//     /// When the attendee is fully registered, send the ticket email.
//     /// </summary>
//     public async ValueTask HandleAsync(AttendeeRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
//     {
//         var jobId = DeterministicGuid.Create($"{domainEvent.Id}:{nameof(SendEmailJobData)}");
//         var emailJobData = new SendEmailJobData(jobId, domainEvent.AttendeeId, EmailTemplateId.Ticket);
//
//         await jobScheduler.AddJobAsync(emailJobData, cancellationToken);
//     }
// }