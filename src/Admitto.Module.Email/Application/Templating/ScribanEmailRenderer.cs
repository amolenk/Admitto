using Amolenk.Admitto.Module.Email.Domain.Entities;
using Scriban;
using Scriban.Runtime;

namespace Amolenk.Admitto.Module.Email.Application.Templating;

internal sealed class ScribanEmailRenderer : IEmailRenderer
{
    public RenderedEmail Render(EmailTemplate template, object parameters)
    {
        var subject  = RenderString(template.Subject,  parameters);
        var textBody = RenderString(template.TextBody, parameters);
        var htmlBody = RenderString(template.HtmlBody, parameters);
        return new RenderedEmail(subject, textBody, htmlBody);
    }

    private static string RenderString(string templateText, object parameters)
    {
        var scribanTemplate = Template.Parse(templateText);
        if (scribanTemplate.HasErrors)
        {
            var errors = string.Join("; ", scribanTemplate.Messages);
            throw new EmailRenderException($"Template parse error: {errors}");
        }

        var context = new TemplateContext { StrictVariables = false };
        var scriptObject = new ScriptObject();
        scriptObject.Import(parameters, renamer: member => StandardMemberRenamer.Rename(member));
        context.PushGlobal(scriptObject);

        try
        {
            return scribanTemplate.Render(context);
        }
        catch (Exception ex)
        {
            throw new EmailRenderException($"Template render error: {ex.Message}", ex);
        }
    }
}
