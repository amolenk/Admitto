using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.GetAttendeeEmails.AdminApi;

namespace Amolenk.Admitto.Core.Email;

public static class EmailModule
{
    public const string Key = nameof(Email);
    public const string NamespacePrefix = "Amolenk.Admitto.Core.Email";

    public static RouteGroupBuilder MapEmailAdminEndpoints(this RouteGroupBuilder group)
    {
        // Event-scoped attendee emails
        group
            .MapGroup("/teams/{teamId:guid}/events/{eventId:guid}/registrations/{registrationId:guid}")
            .WithTags("Admin - Registrations")
            .MapGetAttendeeEmails();

        return group;
    }
}
