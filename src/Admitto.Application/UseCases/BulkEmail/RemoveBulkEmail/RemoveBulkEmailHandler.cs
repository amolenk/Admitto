// using Amolenk.Admitto.Domain.ValueObjects;
//
// namespace Amolenk.Admitto.Application.UseCases.BulkEmail.RemoveBulkEmail;
//
// public class RemoveBulkEmailHandler(IApplicationContext context) : ICommandHandler<RemoveBulkEmailCommand>
// {
//     public async ValueTask HandleAsync(RemoveBulkEmailCommand command, CancellationToken cancellationToken)
//     {
//         var scheduledItems = await context.BulkEmailWorkItems
//             .Where(wi => wi.TicketedEventId == command.TicketedEventId
//                 && wi.EmailType == command.EmailType
//                 && wi.Status == BulkEmailWorkItemStatus.Pending)
//             .ToListAsync(cancellationToken);
//         
//         foreach (var workItem in scheduledItems)
//         {
//             // TODO Test that optimistic concurrency works here
//             context.BulkEmailWorkItems.Remove(workItem);
//         }
//     }
// }