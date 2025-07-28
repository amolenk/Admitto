using Amolenk.Admitto.Application.Common.Email.Templating;
using Amolenk.Admitto.Domain.ValueObjects;
using Scriban;
using Scriban.Runtime;

namespace Amolenk.Admitto.Application.Common.Email.Composing;

/// <summary>
/// Classes that implement this interface can compose email messages in bulk.
/// </summary>
public interface IBulkEmailComposer
{
    // ValueTask<EmailMessage> ComposeMessageAsync(
    //     EmailType emailType,
    //     Guid teamId,
    //     Guid ticketedEventId,
    //     Guid entityId,
    //     Dictionary<string, string>? additionalParameters = null,
    //     CancellationToken cancellationToken = default);
    //
    // ValueTask<EmailMessage> ComposeTestMessageAsync(
    //     EmailType emailType,
    //     Guid teamId,
    //     Guid ticketedEventId,
    //     string recipient,
    //     Dictionary<string, string> additionalDetails,
    //     Dictionary<string, int> tickets,
    //     CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of <see cref="IEmailComposer"/> that composes email messages based on templates.
/// </summary>
// public abstract class BulkEmailComposer(IEmailTemplateService templateService) : IEmailComposer
// {
//     public async ValueTask<EmailMessage> ComposeMessageAsync(
//         EmailType emailType,
//         Guid teamId,
//         Guid ticketedEventId,
//         Guid entityId,
//         Dictionary<string, string>? additionalParameters = null,
//         CancellationToken cancellationToken = default)
//     {
//         var templateParameters = await GetTemplateParametersAsync(
//             entityId,
//             additionalParameters ?? [],
//             cancellationToken);
//
//         return await BuildEmailMessageAsync(emailType, teamId, ticketedEventId, templateParameters, cancellationToken);
//     }
//
//     public async ValueTask<EmailMessage> ComposeTestMessageAsync(
//         EmailType emailType,
//         Guid teamId,
//         Guid ticketedEventId,
//         string recipient,
//         Dictionary<string, string> additionalDetails,
//         Dictionary<string, int> tickets,
//         CancellationToken cancellationToken = default)
//     {
//         var templateParameters = GetTestTemplateParameters(recipient, additionalDetails, tickets);
//
//         return await BuildEmailMessageAsync(emailType, teamId, ticketedEventId, templateParameters, cancellationToken);
//     }
//
//     protected abstract ValueTask<EmailParameters> GetTemplateParametersAsync(
//         Guid entityId,
//         Dictionary<string, string> customParameters,
//         CancellationToken cancellationToken);
//
//     protected abstract EmailParameters GetTestTemplateParameters(
//         string recipient,
//         Dictionary<string, string> additionalDetails,
//         Dictionary<string, int> tickets);
//
//     private async ValueTask<EmailMessage> BuildEmailMessageAsync(
//         EmailType emailType,
//         Guid teamId,
//         Guid ticketedEventId,
//         EmailParameters templateParameters,
//         CancellationToken cancellationToken = default)
//     {
//         var scriptObject = new ScriptObject();
//         scriptObject.Import(templateParameters);
//
//         var templateContext = new TemplateContext();
//         templateContext.PushGlobal(scriptObject);
//
//         var emailTemplate = await templateService.LoadEmailTemplateAsync(
//             emailType,
//             teamId,
//             ticketedEventId,
//             cancellationToken);
//
//         var subject = await RenderTemplateAsync(emailTemplate.Subject, templateContext);
//         var body = await RenderTemplateAsync(emailTemplate.Body, templateContext);
//
//         return new EmailMessage(templateParameters.Email, subject, body, emailType);
//     }
//
//     private static async ValueTask<string> RenderTemplateAsync(string templateContent, TemplateContext templateContext)
//     {
//         var template = Template.Parse(templateContent);
//         if (template.HasErrors)
//         {
//             throw new InvalidOperationException($"Template parsing failed: {string.Join(", ", template.Messages)}");
//         }
//
//         return await template.RenderAsync(templateContext);
//     }
// }