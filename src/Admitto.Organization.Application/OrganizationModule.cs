using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Amolenk.Admitto.Organization.Application;

public static class OrganizationModule
{
    public const string Key = nameof(Organization);
    
    public static RouteGroupBuilder MapOrganizationAdminEndpoints(this RouteGroupBuilder group)
    {
        group
            .MapGroup("/teams/{teamSlug}/events/{eventSlug}");
        
        return group;
    }
}