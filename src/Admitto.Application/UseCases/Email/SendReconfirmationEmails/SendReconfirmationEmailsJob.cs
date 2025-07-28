// using System.Runtime.CompilerServices;
// using Amolenk.Admitto.Application.Common.Cryptography;
// using Amolenk.Admitto.Domain;
// using Amolenk.Admitto.Domain.ValueObjects;
// using Humanizer;
//
// namespace Amolenk.Admitto.Application.UseCases.Email.SendReconfirmationEmails;
//
// // TODO Record reconfirmation sent time and only send emails to attendees who haven't confirmed yet.
//
// public class SendReconfirmationEmailsJob(
//     IApplicationContext context,
//     IEmailTemplateService emailTemplateService,
//     IEmailSender emailSender,
//     ISigningService signingService) 
//     : IJobHandler<SendReconfirmationEmailsJobData>
// {
//     public async ValueTask HandleAsync(
//         SendReconfirmationEmailsJobData job,
//         IJobExecutionContext executionContext,
//         CancellationToken cancellationToken = default)
//     {
//         var templateParameters = await GetAttendeeTemplateParametersAsync(
//             job.TeamId,
//             job.TicketedEventId,
//             cancellationToken);
//         
//         var emailMessages = ComposeEmailMessagesAsync(
//             templateParameters,
//             job.TeamId,
//             job.TicketedEventId,
//             cancellationToken);
//
//         await emailSender.SendEmailsAsync(emailMessages, job.TeamId, cancellationToken);
//     }
//     
//     private async IAsyncEnumerable<EmailMessage> ComposeEmailMessagesAsync(
//         IEnumerable<Dictionary<string, object>> templateParameters,
//         Guid teamId,
//         Guid ticketedEventId,
//         [EnumeratorCancellation] CancellationToken cancellationToken)
//     {
//         foreach (var parameters in templateParameters)
//         {
//             var email = parameters["email"].ToString()!;
//             
//             var (subject, body) = await emailTemplateService.RenderTemplateAsync(
//                 EmailType.Reconfirmation,
//                 parameters,
//                 teamId,
//                 ticketedEventId,
//                 cancellationToken);
//
//             yield return new EmailMessage(email, subject, body);
//         }
//     }
//
//     private async ValueTask<IEnumerable<Dictionary<string, object>>> GetAttendeeTemplateParametersAsync(
//         Guid teamId,
//         Guid eventId,
//         CancellationToken cancellationToken)
//     {
//         var eventInfo = await context.TicketedEvents
//             .AsNoTracking()
//             .Where(e => e.TeamId == teamId && e.Id == eventId)
//             .Select(e => new
//             {
//                 e.Name,
//                 e.Website,
//                 e.BaseUrl
//             })
//             .FirstOrDefaultAsync(cancellationToken: cancellationToken);
//
//         if (eventInfo is null)
//         {
//             throw new BusinessRuleException(BusinessRuleError.TicketedEvent.NotFound(eventId));
//         }
//         
//         var attendeeInfos = await context.Attendees
//             .AsNoTracking()
//             .Where(a => a.TeamId == teamId && a.TicketedEventId == eventId)
//             .Select(a => new
//             {
//                 a.Id,
//                 a.Email,
//                 a.FirstName,
//                 a.LastName,
//                 a.AdditionalDetails
//             })
//             .ToListAsync(cancellationToken: cancellationToken);
//
//         var result = new List<Dictionary<string, object>>();
//         foreach (var attendeeInfo in attendeeInfos)
//         {
//             var templateParameters = new Dictionary<string, object>
//             {
//                 ["event_name"] = eventInfo.Name,
//                 ["event_website"] = eventInfo.Website,
//                 ["email"] = attendeeInfo.Email,
//                 ["first_name"] = attendeeInfo.FirstName,
//                 ["last_name"] = attendeeInfo.LastName,
//                 ["reconfirmation_link"] =
//                     $"{eventInfo.BaseUrl}/reconfirmation/{attendeeInfo.Id}/{signingService.GenerateSignature(attendeeInfo.Id)}",
//                 ["cancellation_link"] =
//                     $"{eventInfo.BaseUrl}/cancel/{attendeeInfo.Id}/{signingService.GenerateSignature(attendeeInfo.Id)}",
//             };
//
//             if (attendeeInfo.AdditionalDetails.Count != 0)
//             {
//                 templateParameters["details"] = attendeeInfo.AdditionalDetails.ToDictionary(
//                     ad => ad.Name.Camelize(),
//                     ad => ad.Value);
//             }
//
//             result.Add(templateParameters);
//         }
//
//         return result;
//     }
// }
