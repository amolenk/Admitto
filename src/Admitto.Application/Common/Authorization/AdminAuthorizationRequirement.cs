using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.Application.Common.Authorization;

/// <summary>
/// Represents an authorization requirement that requires the user to be an administrator.
/// </summary>
public record AdminAuthorizationRequirement : IAuthorizationRequirement;