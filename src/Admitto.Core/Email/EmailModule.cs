using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.GetAttendeeEmails.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CancelBulkEmail.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CreateBulkEmail.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.GetBulkEmail.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.GetBulkEmails.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.PreviewBulkEmail.AdminApi;

namespace Amolenk.Admitto.Core.Email;

public static class EmailModule
{
    public const string Key = nameof(Email);
    public const string NamespacePrefix = "Amolenk.Admitto.Core.Email";

    public static RouteGroupBuilder MapEmailAdminEndpoints(this RouteGroupBuilder group)
    {
        // Event-scoped bulk emails
        group
            .MapGroup("/teams/{teamId:guid}/events/{eventId:guid}/bulk-emails")
            .WithTags("Admin - Bulk Emails")
            .MapPreviewBulkEmail()
            .MapCreateBulkEmail()
            .MapGetBulkEmails()
            .MapGetBulkEmail()
            .MapCancelBulkEmail();

        // Event-scoped attendee emails
        group
            .MapGroup("/teams/{teamId:guid}/events/{eventId:guid}/registrations/{registrationId:guid}")
            .WithTags("Admin - Registrations")
            .MapGetAttendeeEmails();

        return group;
    }
}
