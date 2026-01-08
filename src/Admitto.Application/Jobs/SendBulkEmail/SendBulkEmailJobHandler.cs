// using Amolenk.Admitto.Application.Common.Email;
// using Amolenk.Admitto.Application.Common.Email.Composing;
// using Amolenk.Admitto.Application.Common.Jobs;
// using Amolenk.Admitto.Application.Common.Persistence;
// using Amolenk.Admitto.Domain.Entities;
// using Amolenk.Admitto.Domain.ValueObjects;
//
// namespace Amolenk.Admitto.Application.Jobs.SendBulkEmail;
//
// public class SendBulkEmailJobHandler(
//     IEmailComposerRegistry emailComposerRegistry,
//     IEmailDispatcher emailDispatcher,
//     IApplicationContext context,
//     IUnitOfWork unitOfWork,
//     ILogger<SendBulkEmailJobHandler> logger)
// {
//     public async ValueTask RunAsync(CancellationToken cancellationToken)
//     {
//         logger.LogInformation("Looking for bulk email jobs to process...");
//
//         var jobs = await context.BulkEmailWorkItems
//             .Where(j => j.Status == BulkEmailWorkItemStatus.Pending
//                         || j.Status == BulkEmailWorkItemStatus.PendingRepeat
//                         || j.Status == BulkEmailWorkItemStatus.Error
//                         || j.Status == BulkEmailWorkItemStatus.Running)
//             .ToListAsync(cancellationToken);
//
//         foreach (var job in jobs)
//         {
//             if (!job.TryStart(DateTimeOffset.UtcNow)) continue;
//
//             logger.LogInformation("Running job '{JobId}'...", job.Id);
//
//             // Save unit of work to update job status in database.
//             await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
//
//             try
//             {
//                 await RunJobAsync(job, cancellationToken);
//
//                 job.Complete();
//             }
//             catch (Exception e)
//             {
//                 job.Fail(e);
//             }
//
//             await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
//         }
//     }
//
//     private async ValueTask RunJobAsync(BulkEmailWorkItem workItem, CancellationToken cancellationToken)
//     {
//         var emailComposer = emailComposerRegistry.GetEmailComposer(workItem.EmailType);
//
//         var emailMessages = emailComposer.ComposeBulkMessagesAsync(
//             workItem.EmailType,
//             workItem.TeamId,
//             workItem.TicketedEventId,
//             cancellationToken);
//
//         await emailDispatcher.DispatchEmailsAsync(
//             emailMessages,
//             workItem.TeamId,
//             workItem.TicketedEventId,
//             workItem.Id,
//             cancellationToken: cancellationToken);
//     }
// }