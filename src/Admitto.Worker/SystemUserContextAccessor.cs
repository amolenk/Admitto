using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Contracts;

namespace Amolenk.Admitto.Worker;

/// <summary>
/// Returns a fixed "system" identity for background/worker operations that have no HTTP context.
/// </summary>
internal sealed class SystemUserContextAccessor : IUserContextAccessor
{
    private static readonly UserContextDto SystemUser = new(
        Guid.Empty,
        "system",
        "system@admitto.local");

    public UserContextDto Current => SystemUser;
}
