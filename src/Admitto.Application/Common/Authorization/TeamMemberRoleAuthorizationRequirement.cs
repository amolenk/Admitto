using Amolenk.Admitto.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.Application.Common.Authorization;

/// <summary>
/// Represents an authorization requirement that requires the user to be a team member in the given role.
/// </summary>
public record TeamMemberRoleAuthorizationRequirement(TeamMemberRole RequiredRole, string TeamSlugParameterName)
    : IAuthorizationRequirement;
