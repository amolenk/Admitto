// using Amolenk.Admitto.Application.UseCases.Email.SendEmail;
//
// namespace Amolenk.Admitto.Application.UseCases.Email.SendRegistrationEmail;
//
// public class SendRegistrationEmailJobHandler(
//     IDomainContext domainContext,
//     IEmailTemplateService emailTemplateService,
//     IEmailSender emailSender)
//     : SendEmailJobHandler<SendRegistrationEmailJobData>(emailTemplateService, emailSender)
// {
//     protected override async ValueTask<Dictionary<string, string>> GetTemplateParametersAsync(
//         SendRegistrationEmailJobData jobData,
//         CancellationToken cancellationToken)
//     {
//         var info = await domainContext.PendingRegistrations
//             .AsNoTracking()
//             .Join(
//                 domainContext.TicketedEvents,
//                 r => r.TicketedEventId,
//                 e => e.Id,
//                 (r, e) => new { Registration = r, Event = e })
//             .Where(joined => joined.Registration.Id == jobData.PendingRegistrationId)
//             .Select(joined => new
//             {
//                 joined.Event.Name,
//                 joined.Event.TicketTypes,
//                 joined.Registration.Email,
//                 joined.Registration.FirstName,
//                 joined.Registration.LastName,
//                 joined.Registration.AdditionalDetails,
//                 joined.Registration.Tickets
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