namespace Amolenk.Admitto.Application.UseCases.Email.PreviewEmail;

/// <summary>
/// Represents the response for an email preview.
/// </summary>
public record PreviewEmailResponse(string Subject, string Body);
