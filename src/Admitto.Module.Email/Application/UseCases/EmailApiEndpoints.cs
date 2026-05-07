using Amolenk.Admitto.Module.Email.Application.UseCases.AttendeeEmails.GetAttendeeEmails.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.CancelBulkEmail.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.CreateBulkEmail.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.GetBulkEmail.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.GetBulkEmails.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.PreviewBulkEmail.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.DeleteEmailSettings.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.GetEmailSettings.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.SendTestEmail.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.UpsertEmailSettings.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.CreateEmailTemplate.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.DeleteEmailTemplate.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.GetEmailTemplate.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.GetEmailTemplates.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.PreviewEmailTemplate.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.UpdateEmailTemplate.AdminApi;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases;

public static class EmailApiEndpoints
{
    public static RouteGroupBuilder MapEmailAdminEndpoints(this RouteGroupBuilder group)
    {
        // Team-scoped email settings
        group
            .MapGroup("/teams/{teamSlug}/email-settings")
            .MapGetEmailSettings(EmailSettingsScope.Team, s => s.TeamId)
            .MapUpsertEmailSettings(EmailSettingsScope.Team, s => s.TeamId)
            .MapDeleteEmailSettings(EmailSettingsScope.Team, s => s.TeamId)
            .MapSendTestEmail(EmailSettingsScope.Team, s => s.TeamId);

        // Event-scoped email settings
        group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}/email-settings")
            .MapGetEmailSettings(EmailSettingsScope.Event, s => s.EventId!.Value)
            .MapUpsertEmailSettings(EmailSettingsScope.Event, s => s.EventId!.Value)
            .MapDeleteEmailSettings(EmailSettingsScope.Event, s => s.EventId!.Value)
            .MapSendTestEmail(EmailSettingsScope.Event, s => s.EventId!.Value);

        // Team-scoped email templates
        group
            .MapGroup("/teams/{teamSlug}/email-templates")
            .MapGetEmailTemplates(EmailSettingsScope.Team, s => s.TeamId)
            .MapCreateEmailTemplate(EmailSettingsScope.Team, s => s.TeamId)
            .MapGetEmailTemplate(EmailSettingsScope.Team)
            .MapUpdateEmailTemplate(EmailSettingsScope.Team)
            .MapDeleteEmailTemplate(EmailSettingsScope.Team)
            .MapPreviewEmailTemplate(isEventScoped: false)
            .MapTestSendEmailTemplate(isEventScoped: false);

        // Event-scoped email templates
        group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}/email-templates")
            .MapGetEmailTemplates(EmailSettingsScope.Event, s => s.EventId!.Value)
            .MapCreateEmailTemplate(EmailSettingsScope.Event, s => s.EventId!.Value)
            .MapGetEmailTemplate(EmailSettingsScope.Event)
            .MapUpdateEmailTemplate(EmailSettingsScope.Event)
            .MapDeleteEmailTemplate(EmailSettingsScope.Event)
            .MapPreviewEmailTemplate(isEventScoped: true)
            .MapTestSendEmailTemplate(isEventScoped: true);

        // Event-scoped bulk emails
        group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}/bulk-emails")
            .MapPreviewBulkEmail()
            .MapCreateBulkEmail()
            .MapGetBulkEmails()
            .MapGetBulkEmail()
            .MapCancelBulkEmail();

        // Event-scoped attendee emails
        group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId:guid}")
            .MapGetAttendeeEmails();

        return group;
    }
}
