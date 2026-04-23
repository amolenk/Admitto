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
}
