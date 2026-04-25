using Amolenk.Admitto.Module.Email.Domain.Entities;

namespace Amolenk.Admitto.Module.Email.Application.Templating;

/// <summary>
/// Renders an <see cref="EmailTemplate"/> using Scriban with the supplied parameters.
/// </summary>
internal interface IEmailRenderer
{
    /// <summary>
    /// Renders the template substituting <paramref name="parameters"/> into subject, text body, and HTML body.
    /// </summary>
    /// <exception cref="EmailRenderException">Thrown when the template contains a parse or render error.</exception>
    RenderedEmail Render(EmailTemplate template, object parameters);

    /// <summary>
    /// Renders the template with optional ad-hoc overrides for any of the
    /// three template fields. When a non-null override is provided, it is
    /// rendered through Scriban with the same parameter set (per the
    /// <c>email-templates</c> spec); fields with a null override fall back to
    /// the resolved template's value.
    /// </summary>
    /// <exception cref="EmailRenderException">Thrown when any template or override contains a parse or render error.</exception>
    RenderedEmail Render(
        EmailTemplate template,
        object parameters,
        string? subjectOverride,
        string? textBodyOverride,
        string? htmlBodyOverride);
}
