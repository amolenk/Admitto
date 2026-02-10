// using Amolenk.Admitto.Organization.Application.Persistence;
// using Amolenk.Admitto.Shared.Application.Messaging;
// using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
//
// namespace Amolenk.Admitto.Organization.Application.UseCases.AddTicketType;
//
// internal class AddTicketTypeHandler(IOrganizationWriteStore writeStore)
//     : ICommandHandler<AddTicketTypeCommand>
// {
//     public async ValueTask HandleAsync(AddTicketTypeCommand command, CancellationToken cancellationToken)
//     {
//         var ticketedEvent = await writeStore.TicketedEvents.LoadAggregateAsync(command.EventId, cancellationToken);
//
//         // TODO Add ticket type from command
// //        ticketedEvent.AddAdminGrantTicketType()
//
//         // TODO Dehydrate and persist changes
// //        await writeStore.ApplyChangesAsync(ticketedEvent, cancellationToken);
//
//         throw new NotImplementedException();
//     }
// }