namespace Amolenk.Admitto.Cli.Services;

public interface IAuthService
{
    Task<string?> LoginAsync();
    Task<bool> LogoutAsync();
    Task<string?> GetAccessTokenAsync();
}