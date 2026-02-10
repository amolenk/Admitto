using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.Shared.Application.Auth;

public static class AuthorizationPolicyBuilderExtensions
{
    extension(AuthorizationPolicyBuilder builder)
    {
        public AuthorizationPolicyBuilder RequireAdminRole()
        {
            builder.Requirements.Add(new AdminAuthorizationRequirement());
            return builder;
        }
        
        public AuthorizationPolicyBuilder RequireTeamMembership(TeamMembershipRole role)
        {
            builder.Requirements.Add(new TeamMembershipAuthorizationRequirement(role));
            return builder;
        }
    }
}