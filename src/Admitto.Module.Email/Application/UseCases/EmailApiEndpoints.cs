using Amolenk.Admitto.Module.Email.Application.UseCases.AttendeeEmails.GetAttendeeEmails.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.CancelBulkEmail.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.CreateBulkEmail.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.GetBulkEmail.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.GetBulkEmails.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.PreviewBulkEmail.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.CreateCustomBulkTemplate.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.DeleteCustomBulkTemplate.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.GetCustomBulkTemplate.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.GetCustomBulkTemplates.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.UpdateCustomBulkTemplate.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.DeleteEmailSettings.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.GetEmailSettings.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.SendTestEmail.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.UpsertEmailSettings.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.DeleteEmailTemplate.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.GetEmailTemplate.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.PreviewEmailTemplate.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.UpsertEmailTemplate.AdminApi;
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
            .MapGroup("/teams/{teamSlug}/email-templates/{type}")
            .MapGetEmailTemplate(EmailSettingsScope.Team, s => s.TeamId)
            .MapUpsertEmailTemplate(EmailSettingsScope.Team, s => s.TeamId)
            .MapDeleteEmailTemplate(EmailSettingsScope.Team, s => s.TeamId)
            .MapPreviewEmailTemplate(isEventScoped: false)
            .MapTestSendEmailTemplate(isEventScoped: false);

        // Event-scoped email templates
        group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}/email-templates/{type}")
            .MapGetEmailTemplate(EmailSettingsScope.Event, s => s.EventId!.Value)
            .MapUpsertEmailTemplate(EmailSettingsScope.Event, s => s.EventId!.Value)
            .MapDeleteEmailTemplate(EmailSettingsScope.Event, s => s.EventId!.Value)
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

        // Team-scoped custom bulk templates
        group
            .MapGroup("/teams/{teamSlug}/custom-bulk-templates")
            .MapGetCustomBulkTemplates(EmailSettingsScope.Team, s => s.TeamId)
            .MapCreateCustomBulkTemplate(EmailSettingsScope.Team, s => s.TeamId)
            .MapGetCustomBulkTemplate(EmailSettingsScope.Team)
            .MapUpdateCustomBulkTemplate(EmailSettingsScope.Team)
            .MapDeleteCustomBulkTemplate(EmailSettingsScope.Team);

        // Event-scoped custom bulk templates
        group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}/custom-bulk-templates")
            .MapGetCustomBulkTemplates(EmailSettingsScope.Event, s => s.EventId!.Value)
            .MapCreateCustomBulkTemplate(EmailSettingsScope.Event, s => s.EventId!.Value)
            .MapGetCustomBulkTemplate(EmailSettingsScope.Event)
            .MapUpdateCustomBulkTemplate(EmailSettingsScope.Event)
            .MapDeleteCustomBulkTemplate(EmailSettingsScope.Event);

        // Event-scoped attendee emails
        group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId:guid}")
            .MapGetAttendeeEmails();

        return group;
    }
}
