using Amolenk.Admitto.Core.Shared.Contracts;

namespace Amolenk.Admitto.Core.Shared.Application.Auth;

public interface IUserContextAccessor
{
    UserContextDto Current { get; }
}