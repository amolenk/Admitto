using Amolenk.Admitto.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.Shared.Application.Auth;

public static class AuthorizationPolicyBuilderExtensions
{
    extension(AuthorizationPolicyBuilder builder)
    {
        public AuthorizationPolicyBuilder RequireAdmin()
        {
            builder.Requirements.Add(new AdminAuthorizationRequirement());
            return builder;
        }
        
        public AuthorizationPolicyBuilder RequireTeamMemberRole(RequiredTeamMemberRole role)
        {
            builder.Requirements.Add(new TeamMemberRoleAuthorizationRequirement(role));
            return builder;
        }
    }
}