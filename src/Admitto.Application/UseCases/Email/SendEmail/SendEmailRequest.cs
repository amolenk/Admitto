using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail;

/// <summary>
/// Represents a request to send a single email.
/// </summary>
public record SendEmailRequest(EmailType EmailType, Guid DataEntityId);