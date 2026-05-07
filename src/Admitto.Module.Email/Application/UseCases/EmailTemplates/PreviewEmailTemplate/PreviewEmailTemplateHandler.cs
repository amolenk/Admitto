using Amolenk.Admitto.Module.Email.Application.Templating;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.PreviewEmailTemplate;

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
