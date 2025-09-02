// using Amolenk.Admitto.Application.Common;
// using Amolenk.Admitto.Application.Common.Email;
// using Amolenk.Admitto.Application.UseCases.BulkEmail.ScheduleBulkEmail;
// using Amolenk.Admitto.Domain.ValueObjects;
//
// namespace Amolenk.Admitto.Application.UseCases.BulkEmail.AutoScheduleBulkEmails;
//
// public class AutoScheduleBulkEmailsHandler(
//     IApplicationContext context,
//     ScheduleBulkEmailHandler scheduleBulkEmailHandler) : ICommandHandler<AutoScheduleBulkEmailsCommand>
// {
//     public async ValueTask HandleAsync(AutoScheduleBulkEmailsCommand command, CancellationToken cancellationToken)
//     {
//         var ticketedEvent = await context.TicketedEvents.FindAsync([command.TicketedEventId], cancellationToken);
//         if (ticketedEvent is null)
//         {
//             throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
//         }
//         
//         await ScheduleReconfirmEmails(command.TeamId, ticketedEvent.Id, ticketedEvent.ReconfirmPolicy, cancellationToken);
//     }
//
//     private async ValueTask ScheduleReconfirmEmails(
//         Guid teamId,
//         Guid eventId,
//         ReconfirmPolicy policy,
//         CancellationToken cancellationToken)
//     {
//         var command = new ScheduleBulkEmailCommand(
//             teamId,
//             eventId,
//             WellKnownEmailType.Reconfirm,
//             DateTimeOffset.MinValue,
//             DateTimeOffset.MaxValue);
//
//
//         await scheduleBulkEmailHandler.HandleAsync(command, cancellationToken);
//     }
// }