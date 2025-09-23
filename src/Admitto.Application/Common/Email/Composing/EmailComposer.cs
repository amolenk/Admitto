using System.Runtime.CompilerServices;
using Amolenk.Admitto.Application.Common.Email.Templating;
using Amolenk.Admitto.Domain.ValueObjects;
using Scriban;
using Scriban.Runtime;

namespace Amolenk.Admitto.Application.Common.Email.Composing;

/// <summary>
/// Classes that implement this interface can compose email messages.
/// </summary>
public interface IEmailComposer
{
    ValueTask<EmailMessage> ComposeMessageAsync(
        string emailType,
        Guid teamId,
        Guid ticketedEventId,
        Guid entityId,
        Dictionary<string, string>? additionalParameters = null,
        CancellationToken cancellationToken = default);

    ValueTask<EmailMessage> ComposeTestMessageAsync(
        string emailType,
        Guid teamId,
        Guid ticketedEventId,
        string recipient,
        List<AdditionalDetail> additionalDetails,
        List<TicketSelection> tickets,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<EmailMessage> ComposeBulkMessagesAsync(
        string emailType,
        Guid teamId,
        Guid ticketedEventId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of <see cref="IEmailComposer"/> that composes email messages based on templates.
/// </summary>
public abstract class EmailComposer(IEmailTemplateService templateService) : IEmailComposer
{
    public async ValueTask<EmailMessage> ComposeMessageAsync(
        string emailType,
        Guid teamId,
        Guid ticketedEventId,
        Guid entityId,
        Dictionary<string, string>? additionalParameters = null,
        CancellationToken cancellationToken = default)
    {
        var (templateParameters, participantId) = await GetTemplateParametersAsync(
            ticketedEventId,
            entityId,
            additionalParameters ?? [],
            cancellationToken);

        return await BuildEmailMessageAsync(
            emailType,
            teamId,
            ticketedEventId,
            templateParameters,
            participantId,
            cancellationToken);
    }

    public async ValueTask<EmailMessage> ComposeTestMessageAsync(
        string emailType,
        Guid teamId,
        Guid ticketedEventId,
        string recipient,
        List<AdditionalDetail> additionalDetails,
        List<TicketSelection> tickets,
        CancellationToken cancellationToken = default)
    {
        var templateParameters = await GetTestTemplateParametersAsync(
            ticketedEventId,
            recipient,
            additionalDetails,
            tickets,
            cancellationToken);

        return await BuildEmailMessageAsync(
            emailType,
            teamId,
            ticketedEventId,
            templateParameters,
            cancellationToken: cancellationToken);
    }

    public async IAsyncEnumerable<EmailMessage> ComposeBulkMessagesAsync(
        string emailType,
        Guid teamId,
        Guid ticketedEventId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var entityIds = await GetEntityIdsForBulkAsync(ticketedEventId, cancellationToken);

        foreach (var entityId in entityIds)
        {
            var (templateParameters, participantId) = await GetTemplateParametersAsync(
                ticketedEventId,
                entityId,
                [],
                cancellationToken);

            yield return await BuildEmailMessageAsync(
                emailType,
                teamId,
                ticketedEventId,
                templateParameters,
                participantId,
                cancellationToken);
        }
    }

    protected abstract ValueTask<(IEmailParameters Parameters, Guid? ParticipantId)> GetTemplateParametersAsync(
        Guid ticketedEventId,
        Guid entityId,
        Dictionary<string, string> customParameters,
        CancellationToken cancellationToken);

    protected abstract ValueTask<IEmailParameters> GetTestTemplateParametersAsync(
        Guid ticketedEventId,
        string recipient,
        List<AdditionalDetail> additionalDetails,
        List<TicketSelection> tickets,
        CancellationToken cancellationToken);

    protected virtual ValueTask<IEnumerable<Guid>> GetEntityIdsForBulkAsync(
        Guid ticketedEventId,
        CancellationToken cancellationToken)
    {
        throw new ApplicationRuleException(ApplicationRuleError.Email.BulkNotSupported);
    }

    private async ValueTask<EmailMessage> BuildEmailMessageAsync(
        string emailType,
        Guid teamId,
        Guid ticketedEventId,
        IEmailParameters templateParameters,
        Guid? participantId = null,
        CancellationToken cancellationToken = default)
    {
        var scriptObject = new ScriptObject();
        scriptObject.Import(templateParameters);

        var templateContext = new TemplateContext();
        templateContext.PushGlobal(scriptObject);

        var emailTemplate = await templateService.LoadEmailTemplateAsync(
            emailType,
            teamId,
            ticketedEventId,
            cancellationToken);

        var subject = await RenderTemplateAsync(emailTemplate.Subject, templateContext);
        var textBody = await RenderTemplateAsync(emailTemplate.TextBody, templateContext);
        var htmlBody = await RenderTemplateAsync(emailTemplate.HtmlBody, templateContext);

        return new EmailMessage(
            templateParameters.Recipient,
            subject,
            textBody,
            htmlBody,
            emailType,
            participantId);
    }

    private static async ValueTask<string> RenderTemplateAsync(string templateContent, TemplateContext templateContext)
    {
        var template = Template.Parse(templateContent);
        if (template.HasErrors)
        {
            throw new InvalidOperationException($"Template parsing failed: {string.Join(", ", template.Messages)}");
        }

        return await template.RenderAsync(templateContext);
    }
}