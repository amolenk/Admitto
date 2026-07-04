using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CreateBulkEmail.AdminApi;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.PreviewBulkEmail.AdminApi;

/// <summary>
/// POST /preview request body. Carries the attendee filter to resolve against
/// live Registrations data.
/// </summary>
public sealed record PreviewBulkEmailHttpRequest(
    AttendeeFilterHttpDto AttendeeFilter);
