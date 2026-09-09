using Amolenk.Admitto.Core.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.Composing;

/// <summary>
/// Renders an <see cref="EmailTemplate"/> using a closed, explicit template-value map.
/// </summary>
internal interface IEmailRenderer
{
    /// <summary>
    /// Renders the template substituting <paramref name="parameters"/> into subject, text body, and HTML body.
    /// </summary>
    /// <exception cref="EmailRenderException">Thrown when the template contains a parse or render error.</exception>
    RenderedEmail Render(EmailTemplate template, IReadOnlyDictionary<string, object?> parameters);
}
