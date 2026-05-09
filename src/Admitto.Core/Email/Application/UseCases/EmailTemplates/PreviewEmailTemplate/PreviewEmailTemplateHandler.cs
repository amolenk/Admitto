using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.PreviewEmailTemplate;

internal sealed class PreviewEmailTemplateHandler(IEmailRenderer renderer)
    : IQueryHandler<PreviewEmailTemplateQuery, PreviewEmailTemplateDto>
{
    public ValueTask<PreviewEmailTemplateDto> HandleAsync(
        PreviewEmailTemplateQuery query,
        CancellationToken ct)
    {
        var draftTemplate = EmailTemplate.Create(
            EmailSettingsScope.Team,
            Guid.Empty,
            "preview",
            query.Subject,
            query.TextBody,
            query.HtmlBody);

        var parameters = EmailTemplateSampleParameters.Create();

        RenderedEmail rendered;
        try
        {
            rendered = renderer.Render(draftTemplate, parameters);
        }
        catch (EmailRenderException ex)
        {
            throw new BusinessRuleViolationException(new Error("email_template.render_failed", ex.Message));
        }

        return new ValueTask<PreviewEmailTemplateDto>(
            new PreviewEmailTemplateDto(rendered.Subject, rendered.TextBody, rendered.HtmlBody));
    }
}
