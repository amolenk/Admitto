namespace Amolenk.Admitto.Module.Email.Application.Templating;

/// <summary>
/// Represents the rendered output of an email template.
/// </summary>
internal sealed record RenderedEmail(string Subject, string TextBody, string HtmlBody);
