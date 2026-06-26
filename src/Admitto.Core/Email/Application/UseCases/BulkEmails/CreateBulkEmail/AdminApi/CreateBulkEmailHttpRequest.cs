namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CreateBulkEmail.AdminApi;

/// <summary>
/// POST request body for creating a new custom bulk-email job with direct content.
/// </summary>
public sealed record CreateBulkEmailHttpRequest(
    string EmailType,
    string Subject,
    string TextBody,
    string HtmlBody,
    BulkEmailSourceHttpDto Source);
