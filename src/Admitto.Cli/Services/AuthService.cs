using System.Diagnostics;
using Duende.IdentityModel.Client;

namespace Amolenk.Admitto.Cli.Services;

public class AuthService(ITokenCache tokenCache, IHttpClientFactory clientFactory) : IAuthService
{
    private const string Authority = "http://localhost:8080/realms/admitto";
    private const string ClientId = "admitto-admin-cli";

    public async ValueTask<bool> LoginAsync()
    {
        var client = clientFactory.CreateClient();
        var disco = await GetDiscoveryDocumentAsync(client);
        if (disco is null) return false;

        var deviceResponse = await client.RequestDeviceAuthorizationAsync(new DeviceAuthorizationRequest
        {
            Address = disco.DeviceAuthorizationEndpoint,
            ClientId = ClientId,
            Scope = "api.admin offline_access",
        });

        if (deviceResponse.IsError)
        {
            AnsiConsole.WriteLine($"❌ Device code request failed: {deviceResponse.Error}");
            return false;
        }

        AnsiConsole.WriteLine($"🌍 Opening browser for login...");
        AnsiConsole.WriteLine($"🔗 If the browser does not open, visit: {deviceResponse.VerificationUriComplete}");
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine("⏳ Waiting for login...");

        Process.Start(new ProcessStartInfo
        {
            FileName = deviceResponse.VerificationUriComplete,
            UseShellExecute = true
        });

        var expiration = DateTime.UtcNow.AddSeconds(deviceResponse.ExpiresIn ?? 120);
        var interval = TimeSpan.FromSeconds(deviceResponse.Interval);

        TokenResponse? tokenResponse = null;

        while (DateTime.UtcNow < expiration)
        {
            await Task.Delay(interval);

            tokenResponse = await client.RequestDeviceTokenAsync(new DeviceTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = ClientId,
                DeviceCode = deviceResponse.DeviceCode!
            });

            if (tokenResponse.IsError)
            {
                switch (tokenResponse.Error)
                {
                    case "authorization_pending":
                        continue;
                    case "slow_down":
                        interval += TimeSpan.FromSeconds(5);
                        continue;
                    default:
                        AnsiConsole.WriteLine($"Token request error: {tokenResponse.Error}");
                        return false;
                }
            }

            break;
        }

        if (tokenResponse == null || tokenResponse.IsError)
        {
            AnsiConsole.WriteLine("Authorization timed out or failed.");
            return false;
        }

        CacheToken(tokenResponse);

        return true;
    }

    public void Logout() => tokenCache.Clear();

    public async Task<string?> GetAccessTokenAsync()
    {
        var cachedToken = tokenCache.Load();
        if (cachedToken is null) return null;

        if (cachedToken.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
        {
            return cachedToken.AccessToken;
        }

        if (string.IsNullOrEmpty(cachedToken.RefreshToken)) return null;

        var client = clientFactory.CreateClient();
        var disco = await GetDiscoveryDocumentAsync(client);
        if (disco is null) return null;

        var tokenResponse = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = ClientId,
            RefreshToken = cachedToken.RefreshToken
        });

        if (tokenResponse.IsError)
        {
            AnsiConsole.WriteLine($"❌ Refreshing token failed: {tokenResponse.Error}");
            return null;
        }

        CacheToken(tokenResponse);

        return tokenResponse.AccessToken;
    }

    private async Task<DiscoveryDocumentResponse?> GetDiscoveryDocumentAsync(HttpClient client)
    {
        var disco = await client.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
        {
            Address = Authority,
            Policy = new DiscoveryPolicy
            {
                RequireHttps = false,
                ValidateIssuerName = false
            }
        });

        if (disco.IsError)
        {
            AnsiConsole.WriteLine($"❌ Discovery failed: {disco.Error}");
            return null;
        }

        return disco;
    }

    private void CacheToken(TokenResponse tokenResponse)
    {
        tokenCache.Save(new CachedToken
        {
            AccessToken = tokenResponse.AccessToken!,
            RefreshToken = tokenResponse.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
        });
    }
}
