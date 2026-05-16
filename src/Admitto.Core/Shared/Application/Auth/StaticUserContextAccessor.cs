using Amolenk.Admitto.Core.Shared.Contracts;

namespace Amolenk.Admitto.Core.Shared.Application.Auth;

/// <summary>
/// Returns a fixed <see cref="UserContextDto"/> for contexts that have no
/// runtime identity (background workers, migrations, tests).
/// </summary>
public sealed class StaticUserContextAccessor(UserContextDto user) : IUserContextAccessor
{
    public static readonly UserContextDto SystemUser = new(Guid.Empty, "system", "system@admitto.local");

    public UserContextDto Current => user;
}
