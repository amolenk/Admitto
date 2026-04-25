namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.CreateBulkEmail.AdminApi;

/// <summary>
/// POST request body for creating a new bulk-email job. Either an
/// <see cref="EmailType"/> resolving to a stored template, or ad-hoc
/// <see cref="Subject"/>/<see cref="TextBody"/>/<see cref="HtmlBody"/>
/// overrides may be supplied; the validator enforces the policy.
/// </summary>
public sealed record CreateBulkEmailHttpRequest(
    string EmailType,
    string? Subject,
    string? TextBody,
    string? HtmlBody,
    BulkEmailSourceHttpDto Source);
