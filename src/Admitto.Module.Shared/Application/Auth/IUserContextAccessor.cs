using Amolenk.Admitto.Module.Shared.Contracts;

namespace Amolenk.Admitto.Module.Shared.Application.Auth;

public interface IUserContextAccessor
{
    UserContextDto Current { get; }
}