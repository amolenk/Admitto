namespace Amolenk.Admitto.Application.UseCases.BulkEmail.SendReconfirmBulkEmail;

/// <summary>
/// Represents a request to send a reconfirm bulk email.
/// </summary>
public record SendReconfirmBulkEmailRequest(
    TimeSpan InitialDelayAfterRegistration,
    TimeSpan? ReminderInterval);
