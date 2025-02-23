// using Amolenk.Admitto.Domain.DomainEvents;
//
// namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee.EventHandlers;
//
// public class RegistrationAcceptedDomainEventHandler(SendAcceptanceEmailHandler sendAcceptanceEmailHandler)
//     : INotificationHandler<RegistrationFinalizedDomainEvent>
// {
//     public Task Handle(RegistrationFinalizedDomainEvent notification, CancellationToken cancellationToken)
//     {
//         return sendAcceptanceEmailHandler.Handle();
//     }
// }