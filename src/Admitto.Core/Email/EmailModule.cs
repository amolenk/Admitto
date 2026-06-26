using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.GetAttendeeEmails.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CancelBulkEmail.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CreateBulkEmail.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.GetBulkEmail.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.GetBulkEmails.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.PreviewBulkEmail.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.DeleteEmailSettings.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.GetEmailSettings.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.SendTestEmail.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.UpsertEmailSettings.AdminApi;

namespace Amolenk.Admitto.Core.Email;

public static class EmailModule
{
    public const string Key = nameof(Email);
    public const string NamespacePrefix = "Amolenk.Admitto.Core.Email";

    public static RouteGroupBuilder MapEmailAdminEndpoints(this RouteGroupBuilder group)
    {
        // Team-scoped email settings
        group
            .MapGroup("/teams/{teamId:guid}/email-settings")
            .WithTags("Admin - Email Settings")
            .MapGetEmailSettings()
            .MapUpsertEmailSettings()
            .MapDeleteEmailSettings()
            .MapSendTestEmail();

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
