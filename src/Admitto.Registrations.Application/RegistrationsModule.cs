using Amolenk.Admitto.Registrations.Application.UseCases.RegisterAttendee.Admin;

namespace Amolenk.Admitto.Registrations.Application;

public static class RegistrationsModule
{
    public const string Key = nameof(Registrations);
    
    public static RouteGroupBuilder MapRegistrationsAdminEndpoints(this RouteGroupBuilder group)
    {
        group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}")
            .MapRegisterAttendee();
        
        return group;
    }
}