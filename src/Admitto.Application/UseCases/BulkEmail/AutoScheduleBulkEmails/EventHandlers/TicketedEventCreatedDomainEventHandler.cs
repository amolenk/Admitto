// using Amolenk.Admitto.Domain.DomainEvents;
//
// namespace Amolenk.Admitto.Application.UseCases.BulkEmail.AutoScheduleBulkEmails.EventHandlers;
//
// /// <summary>
// /// When a ticketed event is created, schedule the bulk email work items.
// /// </summary>
// public class TicketedEventCreatedDomainEventHandler(AutoScheduleBulkEmailsHandler autoScheduleBulkEmailsHandler)
//     : IEventualDomainEventHandler<TicketedEventCreatedDomainEvent>
// {
//     public async ValueTask HandleAsync(TicketedEventCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
//     {
//         var command = new AutoScheduleBulkEmailsCommand(domainEvent.TeamId, domainEvent.TicketedEventId);
//
//         await autoScheduleBulkEmailsHandler.HandleAsync(command, cancellationToken);
//     }
// }