// namespace Amolenk.Admitto.Application.UseCases.Email.SendAttendeeEmail;
//
// public class SendAttendeeEmailJobHandler(
//     IDomainContext domainContext,
//     IEmailTemplateService emailTemplateService,
//     IEmailSender emailSender)
//     : IJobHandler<SendAttendeeEmailJobData>
// {
//     public async ValueTask HandleAsync(
//         SendAttendeeEmailJobData jobData,
//         IJobExecutionContext executionContext,
//         CancellationToken cancellationToken = default)
//     {
//         var templateParameters = await GetTemplateParametersAsync(
//             jobData.AttendeeId,
//             cancellationToken);
//         
//         var (subject, body) = await emailTemplateService.RenderTemplateAsync(
//             jobData.EmailType,
//             templateParameters,
//             jobData.TeamId,
//             jobData.TicketedEventId,
//             cancellationToken);
//         
//         await emailSender.SendEmailAsync(
//             templateParameters["email"],
//             subject,
//             body,
//             jobData.TeamId);
//     }
//     
//     private async ValueTask<Dictionary<string, string>> GetTemplateParametersAsync(
//         Guid attendeeId,
//         CancellationToken cancellationToken)
//     {
//         var info = await domainContext.Attendees
//             .AsNoTracking()
//             .Join(
//                 domainContext.TicketedEvents,
//                 a => a.TicketedEventId,
//                 e => e.Id,
//                 (a, e) => new { Attendee = a, Event = e })
//             .Where(joined => joined.Attendee.Id == attendeeId)
//             .Select(joined => new
//             {
//                 joined.Event.Name,
//                 joined.Event.TicketTypes,
//                 joined.Attendee.Email,
//                 joined.Attendee.FirstName,
//                 joined.Attendee.LastName,
//                 joined.Attendee.AdditionalDetails,
//                 joined.Attendee.Tickets
//             })
//             .FirstOrDefaultAsync(cancellationToken: cancellationToken);
//
//         if (info is null)
//         {
//             // TODO
//             throw new Exception("Registration not found");
//         }
//         
//         var templateParameters = new Dictionary<string, string>
//         {
//             ["event_name"] = info.Name,
//             ["email"] = info.Email,
//             ["first_name"] = info.FirstName,
//             ["last_name"] = info.LastName
//         };
//
//         // TODO Add ticket details to template parameters
//         // TODO Add additional details to template parameters
//         
//         return templateParameters;
//     }
// }