// using Amolenk.Admitto.Application.UseCases.TicketedEventsAvailability.ReleaseTickets;
// using Amolenk.Admitto.Domain.Entities;
// using Amolenk.Admitto.Domain.ValueObjects;
//
// namespace Amolenk.Admitto.Application.UseCases.Attendees.CompleteRegistration;
//
// /// <summary>
// /// Completes the registration process for an attendee by creating a new attendee aggregate.
// /// </summary>
// public class CompleteRegistrationHandler(IApplicationContext context, IMessageOutbox outbox)
//     : ICommandHandler<CompleteRegistrationCommand>
// {
//     public async ValueTask HandleAsync(CompleteRegistrationCommand command, CancellationToken cancellationToken)
//     {
//         var existingAttendee = await context.Attendees.FirstOrDefaultAsync(
//             a => a.ParticipantId == command.ParticipantId,
//             cancellationToken);
//
//         if (existingAttendee is not null)
//         {
//             // If a previously cancelled registration exists, remove it now to avoid a conflict.
//             if (existingAttendee.RegistrationStatus == RegistrationStatus.Canceled)
//             {
//                 context.Attendees.Remove(existingAttendee);
//             }
//             else
//             {
//                 // Houston we've got a problem.
//                 // We've got tickets claimed but an active registration already exists. This should not happen often
//                 // because we also check for existing registrations when claiming tickets.
//                 // Schedule a command to release the tickets again.
//                 outbox.Enqueue(new ReleaseTicketsCommand(command.TicketedEventId, command.ClaimedTickets));
//                 return;
//             }
//         }
//
//         // Create the new attendee registration.
//         // We can still fail at this point if a concurrent registration was created in the meantime, but that is very
//         // unlikely. In that case the command will be retried and tickets will be released again.
//         var newAttendee = Attendee.Create(
//             command.TicketedEventId,
//             command.ParticipantId,
//             command.Email,
//             command.FirstName,
//             command.LastName,
//             command.AdditionalDetails,
//             command.ClaimedTickets);
//
//         context.Attendees.Add(newAttendee);
//     }
// }