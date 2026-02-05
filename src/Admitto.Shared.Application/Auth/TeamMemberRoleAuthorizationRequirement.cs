using Amolenk.Admitto.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.Shared.Application.Auth;

/// <summary>
/// Represents an authorization requirement that requires the user to be a team member in the given role.
/// </summary>
public record TeamMemberRoleAuthorizationRequirement(RequiredTeamMemberRole RequiredRole)
    : IAuthorizationRequirement;
