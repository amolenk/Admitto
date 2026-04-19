using Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.GetEventEmailSettings.AdminApi;
using Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.UpsertEventEmailSettings.AdminApi;

namespace Amolenk.Admitto.Module.Email.Application.UseCases;

public static class EmailApiEndpoints
{
    public static RouteGroupBuilder MapEmailAdminEndpoints(this RouteGroupBuilder group)
    {
        var emailSettings = group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}/email-settings");

        emailSettings
            .MapUpsertEventEmailSettings()
            .MapGetEventEmailSettings();

        return group;
    }
}
