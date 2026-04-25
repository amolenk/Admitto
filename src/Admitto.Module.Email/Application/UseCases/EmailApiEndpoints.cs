using Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.CancelBulkEmail.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.CreateBulkEmail.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.GetBulkEmail.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.GetBulkEmails.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.DeleteEmailSettings.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.GetEmailSettings.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.UpsertEmailSettings.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.DeleteEmailTemplate.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.GetEmailTemplate.AdminApi;
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
            .MapDeleteEmailSettings(EmailSettingsScope.Team, s => s.TeamId);

        // Event-scoped email settings
        group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}/email-settings")
            .MapGetEmailSettings(EmailSettingsScope.Event, s => s.EventId!.Value)
            .MapUpsertEmailSettings(EmailSettingsScope.Event, s => s.EventId!.Value)
            .MapDeleteEmailSettings(EmailSettingsScope.Event, s => s.EventId!.Value);

        // Team-scoped email templates
        group
            .MapGroup("/teams/{teamSlug}/email-templates/{type}")
            .MapGetEmailTemplate(EmailSettingsScope.Team, s => s.TeamId)
            .MapUpsertEmailTemplate(EmailSettingsScope.Team, s => s.TeamId)
            .MapDeleteEmailTemplate(EmailSettingsScope.Team, s => s.TeamId);

        // Event-scoped email templates
        group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}/email-templates/{type}")
            .MapGetEmailTemplate(EmailSettingsScope.Event, s => s.EventId!.Value)
            .MapUpsertEmailTemplate(EmailSettingsScope.Event, s => s.EventId!.Value)
            .MapDeleteEmailTemplate(EmailSettingsScope.Event, s => s.EventId!.Value);

        // Event-scoped bulk emails
        group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}/bulk-emails")
            .MapCreateBulkEmail()
            .MapGetBulkEmails()
            .MapGetBulkEmail()
            .MapCancelBulkEmail();

        return group;
    }
}
