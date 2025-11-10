namespace Amolenk.Admitto.Application.UseCases.BulkEmail.SendCustomBulkEmail;

/// <summary>
/// Represents a request to send a bulk email.
/// </summary>
public record SendCustomBulkEmailRequest(
    string EmailType,
    string RecipientListName,
    bool ExcludeAttendees,
    string? IdempotencyKey,
    TestOptionsDto? TestOptions);
    
public record TestOptionsDto(string Recipient, int MaxEmailCount);