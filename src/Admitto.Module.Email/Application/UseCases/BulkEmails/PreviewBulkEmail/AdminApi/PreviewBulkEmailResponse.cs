namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.PreviewBulkEmail.AdminApi;

/// <summary>
/// Response for the preview endpoint. Returns the total recipient
/// count and a sample (up to 100) of recipients with email and display name.
/// </summary>
public sealed record PreviewBulkEmailResponse(
    int Count,
    IReadOnlyList<BulkEmailRecipientPreviewDto> Sample);

public sealed record BulkEmailRecipientPreviewDto(
    string Email,
    string? DisplayName);
