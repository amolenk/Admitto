using Amolenk.Admitto.Module.Email.Application.Templating;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.PreviewEmailTemplate;

internal sealed class PreviewEmailTemplateHandler(
    IEmailTemplateService templateService,
    IEmailRenderer renderer)
    : IQueryHandler<PreviewEmailTemplateQuery, PreviewEmailTemplateDto>
{
    private static readonly Error TemplateNotAvailable = new(
        "email_template.not_available",
        "No template is available for this type. Configure a custom template first.");

    public async ValueTask<PreviewEmailTemplateDto> HandleAsync(
        PreviewEmailTemplateQuery query,
        CancellationToken ct)
    {
        EmailTemplate template;
        try
        {
            template = query.EventId.HasValue
                ? await templateService.LoadAsync(query.Type, query.TeamId, query.EventId.Value, ct)
                : await templateService.LoadAsync(query.Type, query.TeamId, ct);
        }
        catch (InvalidOperationException)
        {
            throw new BusinessRuleViolationException(TemplateNotAvailable);
        }

        var parameters = EmailTemplateSampleParameters.Create();

        RenderedEmail rendered;
        try
        {
            rendered = renderer.Render(template, parameters);
        }
        catch (EmailRenderException ex)
        {
            throw new BusinessRuleViolationException(new Error("email_template.render_failed", ex.Message));
        }

        return new PreviewEmailTemplateDto(rendered.Subject, rendered.TextBody, rendered.HtmlBody);
    }
}
