namespace Amolenk.Admitto.Cli.Services;

public interface IAuthService
{
    ValueTask<bool> LoginAsync();
    void Logout();
    Task<string?> GetAccessTokenAsync();
}