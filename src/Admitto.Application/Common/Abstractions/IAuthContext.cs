using Amolenk.Admitto.Application.UseCases.Auth;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IAuthContext
{
    DbSet<AuthorizationCode> AuthorizationCodes { get; }
    
    DbSet<RefreshToken> RefreshTokens { get; }
}