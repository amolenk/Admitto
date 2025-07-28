namespace Amolenk.Admitto.Application.UseCases.Email.PreviewEmail;

/// <summary>
/// Represents a request to get a preview of an email.
/// </summary>
public record PreviewEmailRequest(Guid DataEntityId, Dictionary<string, string>? AdditionalParameters);
