using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail;

public record SendEmailRequest(EmailType EmailType, Guid DataEntityId, string? RecipientEmail = null);