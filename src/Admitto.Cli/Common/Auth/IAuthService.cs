namespace Amolenk.Admitto.Cli.Common.Auth;

public interface IAuthService
{
    ValueTask<bool> LoginAsync();
    void Logout();
    Task<string?> GetAccessTokenAsync();
}