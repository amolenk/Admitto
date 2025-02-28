// using Amolenk.Admitto.Domain.DomainEvents;
// using Amolenk.Admitto.Domain.Utilities;
//
// namespace Amolenk.Admitto.Application.Features.Attendees.LogActivity.EventHandlers;
//
// /// <summary>
// /// Write to the activity log when a registration is rejected.
// /// The command ID is generated deterministically based on the domain event ID to ensure idempotency.
// /// </summary>
// public class RegistrationRejectedDomainEventHandler(ISender mediator) : INotificationHandler<RegistrationRejectedDomainEvent>
// {
//     public Task Handle(RegistrationRejectedDomainEvent notification, CancellationToken cancellationToken)
//     {
//         return mediator.Send(
//             new LogActivityCommand(notification.AttendeeId, "Registration rejected", notification.OccurredOn)
//             {
//                 CommandId = DeterministicGuidGenerator.Generate($"{notification.DomainEventId}:LogActivity")
//             },
//             cancellationToken);
//     }
// }
