using Amolenk.Admitto.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.Application.Common.Authorization;

public static class AuthorizationPolicyBuilderExtensions
{
    extension(AuthorizationPolicyBuilder builder)
    {
        public AuthorizationPolicyBuilder RequireAdmin()
        {
            builder.Requirements.Add(new AdminAuthorizationRequirement());
            return builder;
        }
        
        public AuthorizationPolicyBuilder RequireTeamMemberRole(
            TeamMemberRole role,
            string teamSlugParameterName = "teamSlug")
        {
            builder.Requirements.Add(new TeamMemberRoleAuthorizationRequirement(role, teamSlugParameterName));
            return builder;
        }
    }
}