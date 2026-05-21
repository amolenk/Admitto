using Amolenk.Admitto.Core.Email.Application.UseCases.AttendeeEmails.GetAttendeeEmails.AdminApi;
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
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;

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
            .MapGetEmailSettings(EmailSettingsScope.Team)
            .MapUpsertEmailSettings(EmailSettingsScope.Team)
            .MapDeleteEmailSettings(EmailSettingsScope.Team)
            .MapSendTestEmail(EmailSettingsScope.Team);

        // Event-scoped email settings
        group
            .MapGroup("/teams/{teamId:guid}/events/{eventId:guid}/email-settings")
            .MapGetEmailSettings(EmailSettingsScope.Event)
            .MapUpsertEmailSettings(EmailSettingsScope.Event)
            .MapDeleteEmailSettings(EmailSettingsScope.Event)
            .MapSendTestEmail(EmailSettingsScope.Event);

        // Team-scoped email templates
        group
            .MapGroup("/teams/{teamId:guid}/email-templates")
            .MapGetEmailTemplates(EmailSettingsScope.Team)
            .MapCreateEmailTemplate(EmailSettingsScope.Team)
            .MapGetEmailTemplate(EmailSettingsScope.Team)
            .MapUpdateEmailTemplate(EmailSettingsScope.Team)
            .MapDeleteEmailTemplate(EmailSettingsScope.Team)
            .MapPreviewEmailTemplate(isEventScoped: false)
            .MapTestSendEmailTemplate(isEventScoped: false);

        // Event-scoped email templates
        group
            .MapGroup("/teams/{teamId:guid}/events/{eventId:guid}/email-templates")
            .MapGetEmailTemplates(EmailSettingsScope.Event)
            .MapCreateEmailTemplate(EmailSettingsScope.Event)
            .MapGetEmailTemplate(EmailSettingsScope.Event)
            .MapUpdateEmailTemplate(EmailSettingsScope.Event)
            .MapDeleteEmailTemplate(EmailSettingsScope.Event)
            .MapPreviewEmailTemplate(isEventScoped: true)
            .MapTestSendEmailTemplate(isEventScoped: true);

        // Event-scoped bulk emails
        group
            .MapGroup("/teams/{teamId:guid}/events/{eventId:guid}/bulk-emails")
            .MapPreviewBulkEmail()
            .MapCreateBulkEmail()
            .MapGetBulkEmails()
            .MapGetBulkEmail()
            .MapCancelBulkEmail();

        // Event-scoped attendee emails
        group
            .MapGroup("/teams/{teamId:guid}/events/{eventId:guid}/registrations/{registrationId:guid}")
            .MapGetAttendeeEmails();

        return group;
    }
}
