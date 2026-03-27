using Microsoft.AspNetCore.Authorization;

namespace Amolenk.Admitto.Module.Shared.Application.Auth;

/// <summary>
/// Represents an authorization requirement that requires the user to be an administrator.
/// </summary>
public record AdminAuthorizationRequirement : IAuthorizationRequirement;