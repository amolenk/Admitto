namespace Amolenk.Admitto.Application.UseCases.Email.SendReconfirmEmail;

/// <summary>
/// Represents a request to send a reconfirm email.
/// </summary>
public record SendReconfirmEmailRequest(Guid AttendeeId);