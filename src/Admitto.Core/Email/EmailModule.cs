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
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.CreateEmailTemplate.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.DeleteEmailTemplate.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.GetEmailTemplate.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.GetEmailTemplates.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.PreviewEmailTemplate.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate.AdminApi;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.UpdateEmailTemplate.AdminApi;

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
            .MapGetEmailSettings(isEventScoped: false)
            .MapUpsertEmailSettings(isEventScoped: false)
            .MapDeleteEmailSettings(isEventScoped: false)
            .MapSendTestEmail(isEventScoped: false);

        // Event-scoped email settings
        group
            .MapGroup("/teams/{teamId:guid}/events/{eventId:guid}/email-settings")
            .WithTags("Admin - Email Settings")
            .MapGetEmailSettings(isEventScoped: true)
            .MapUpsertEmailSettings(isEventScoped: true)
            .MapDeleteEmailSettings(isEventScoped: true)
            .MapSendTestEmail(isEventScoped: true);

        // Team-scoped email templates
        group
            .MapGroup("/teams/{teamId:guid}/email-templates")
            .WithTags("Admin - Email Templates")
            .MapGetEmailTemplates(isEventScoped: false)
            .MapCreateEmailTemplate(isEventScoped: false)
            .MapGetEmailTemplate(isEventScoped: false)
            .MapUpdateEmailTemplate(isEventScoped: false)
            .MapDeleteEmailTemplate(isEventScoped: false)
            .MapPreviewEmailTemplate(isEventScoped: false)
            .MapTestSendEmailTemplate(isEventScoped: false);

        // Event-scoped email templates
        group
            .MapGroup("/teams/{teamId:guid}/events/{eventId:guid}/email-templates")
            .WithTags("Admin - Email Templates")
            .MapGetEmailTemplates(isEventScoped: true)
            .MapCreateEmailTemplate(isEventScoped: true)
            .MapGetEmailTemplate(isEventScoped: true)
            .MapUpdateEmailTemplate(isEventScoped: true)
            .MapDeleteEmailTemplate(isEventScoped: true)
            .MapPreviewEmailTemplate(isEventScoped: true)
            .MapTestSendEmailTemplate(isEventScoped: true);

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
