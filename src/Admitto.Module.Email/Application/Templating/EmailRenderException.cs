namespace Amolenk.Admitto.Module.Email.Application.Templating;

/// <summary>
/// Thrown when a Scriban template fails to parse or render.
/// This is a deterministic failure (bad template content) and should NOT be retried.
/// </summary>
public sealed class EmailRenderException(string message, Exception? innerException = null)
    : Exception(message, innerException);
