using Microsoft.AspNetCore.Authorization;
using IAuthorizationService = Amolenk.Admitto.Application.Common.Abstractions.IAuthorizationService;

namespace Amolenk.Admitto.Application.Common.Authorization;

/// <summary>
/// Represents an authorization requirement that can be used to check permissions.
/// </summary>
public record AuthorizationRequirement(Func<IAuthorizationService, HttpContext, ValueTask<bool>> Check)
    : IAuthorizationRequirement;