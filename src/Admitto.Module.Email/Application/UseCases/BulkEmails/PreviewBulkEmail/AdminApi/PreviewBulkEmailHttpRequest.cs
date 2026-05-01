using Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.CreateBulkEmail.AdminApi;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.PreviewBulkEmail.AdminApi;

/// <summary>
/// POST /preview request body. Carries exactly one source shape
/// (attendee or externalList); the validator enforces that constraint.
/// </summary>
public sealed record PreviewBulkEmailHttpRequest(
    BulkEmailSourceHttpDto Source);
