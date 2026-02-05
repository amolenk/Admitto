using Amolenk.Admitto.Shared.Contracts;

namespace Amolenk.Admitto.Shared.Application.Auth;

public interface IUserContextAccessor
{
    UserContextDto Current { get; }
}