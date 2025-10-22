namespace Amolenk.Admitto.Cli.Common.Auth;

public class AccessTokenProvider(IAuthService authService) : IAccessTokenProvider
{
    public AllowedHostsValidator AllowedHostsValidator { get; } = null!;
    
    public async Task<string> GetAuthorizationTokenAsync(Uri uri, 
        Dictionary<string, object>? additionalAuthenticationContext = null, 
        CancellationToken cancellationToken = default)
    {
        var token = await authService.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("Authentication required. Please login first.");
        }
        
        return token;
    }
}