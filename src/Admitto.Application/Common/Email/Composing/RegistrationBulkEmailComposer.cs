// using System.Runtime.CompilerServices;
// using Amolenk.Admitto.Application.Common.Cryptography;
// using Amolenk.Admitto.Application.Common.Email.Composing;
// using Amolenk.Admitto.Application.Common.Email.Templating;
// using Amolenk.Admitto.Domain;
// using Amolenk.Admitto.Domain.ValueObjects;
// using Humanizer;
//
// namespace Amolenk.Admitto.Application.Common.Email.ParametersProviders;
//
// public class AttendeeBulkEmailComposer(
//     IApplicationContext context,
//     IEmailTemplateService emailTemplateService,
//     ISigningService signingService) : IEmailParametersProvider
// {
//     public ValueTask<EmailParameters> GetEmailParametersAsync(Guid entityId, CancellationToken cancellationToken)
//     {
//         throw new NotImplementedException();
//     }
//
//     public EmailParameters GetTestEmailParameters(string recipient, Dictionary<string, string> additionalDetails, Dictionary<string, int> tickets)
//     {
//         throw new NotImplementedException();
//     }
//
//     
//     public async IAsyncEnumerable<EmailMessage> ComposeEmailMessagesAsync(
//         Guid teamId,
//         Guid ticketedEventId,
//         [EnumeratorCancellation] CancellationToken cancellationToken = default)
//     {
//         var templateParameters = await GetTemplateParametersAsync(
//             teamId,
//             ticketedEventId,
//             cancellationToken);
//
//         foreach (var parameters in templateParameters)
//         {
//             var (subject, body) = await emailTemplateService.RenderTemplateAsync(
//                 EmailType.Reconfirm,
//                 parameters,
//                 teamId,
//                 ticketedEventId,
//                 cancellationToken);
//
//             yield return new EmailMessage(parameters.Email, subject, body);
//         }
//     }
//
//     private async ValueTask<IEnumerable<object>> GetTemplateParametersAsync(
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
//         var result = new List<AttendeeEmailParameters>();
//         foreach (var attendeeInfo in attendeeInfos)
//         {
//             var parameters = new AttendeeEmailParameters(
//                 eventInfo.Name,
//                 eventInfo.Website,
//                 attendeeInfo.Email,
//                 attendeeInfo.Id.ToString(),
//                 attendeeInfo.FirstName,
//                 attendeeInfo.LastName,
//                 $"{eventInfo.BaseUrl}/tickets/qrcode/attendee/{attendeeInfo.Id}/{signingService.GenerateSignature(attendeeInfo.Id)}",
//                 $"{eventInfo.BaseUrl}/tickets/reconfirm/{attendeeInfo.Id}/{signingService.GenerateSignature(attendeeInfo.Id)}",
//                 $"{eventInfo.BaseUrl}/tickets/cancel/{attendeeInfo.Id}/{signingService.GenerateSignature(attendeeInfo.Id)}",
//                 attendeeInfo.AdditionalDetails.ToDictionary(ad => ad.Name.Camelize(), ad => ad.Value),
//                 null); // Tickets are not included in this email
//
//             result.Add(parameters);
//         }
//
//         return result;
//     }
//
// }