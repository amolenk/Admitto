using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Scriban;
using Scriban.Runtime;

namespace Amolenk.Admitto.Core.Email.Application.Composing;

internal sealed class ScribanEmailRenderer : IEmailRenderer
{
    public RenderedEmail Render(EmailTemplate template, IReadOnlyDictionary<string, object?> parameters)
    {
        var subject  = RenderString(template.Subject, parameters);
        var textBody = RenderString(template.TextBody, parameters);
        var htmlBody = RenderString(template.HtmlBody, parameters);
        return new RenderedEmail(subject, textBody, htmlBody);
    }

    private static string RenderString(string templateText, IReadOnlyDictionary<string, object?> parameters)
    {
        var scribanTemplate = Template.Parse(templateText);
        if (scribanTemplate.HasErrors)
        {
            var errors = string.Join("; ", scribanTemplate.Messages);
            throw new EmailRenderException($"Template parse error: {errors}");
        }

        var context = new TemplateContext { StrictVariables = true };
        var scriptObject = new ScriptObject();
        foreach (var (key, value) in parameters)
        {
            scriptObject.SetValue(key, value, readOnly: true);
        }
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
