using System.Diagnostics;
using Duende.IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Cli.Services;

public class AuthService(ITokenCache tokenCache, IHttpClientFactory clientFactory, IOptions<CliAuthOptions> options) : IAuthService
{
    private readonly CliAuthOptions _options = options.Value;
    
    public async ValueTask<bool> LoginAsync()
    {
        var client = clientFactory.CreateClient();
        var disco = await GetDiscoveryDocumentAsync(client);
        if (disco is null) return false;

        var deviceResponse = await client.RequestDeviceAuthorizationAsync(new DeviceAuthorizationRequest
        {
            Address = disco.DeviceAuthorizationEndpoint,
            ClientId = _options.ClientId,
            ClientCredentialStyle = ClientCredentialStyle.PostBody, // For Entra
            Scope = _options.Scope,
        });

        if (deviceResponse.IsError)
        {
            AnsiConsole.WriteLine($"❌ Device code request failed: {deviceResponse.Error}");
            if (deviceResponse.Raw is not null)
            {
                AnsiConsole.WriteLine(deviceResponse.Raw);
            }

            return false;
        }

        if (!string.IsNullOrWhiteSpace(deviceResponse.VerificationUriComplete))
        {
            AnsiConsole.WriteLine($"🌍 Opening browser for login...");
            AnsiConsole.WriteLine($"🔗 If the browser does not open, visit: {deviceResponse.VerificationUriComplete}");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("⏳ Waiting for login...");

            Process.Start(new ProcessStartInfo
            {
                FileName = deviceResponse.VerificationUriComplete,
                UseShellExecute = true
            });
        }
        else
        {
            AnsiConsole.MarkupLine($"To sign in, use a web browser to open the page [link={deviceResponse.VerificationUri}]{deviceResponse.VerificationUri}[/] and enter the code {deviceResponse.UserCode} to authenticate.");
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine("⏳ Waiting for login...");
        }
        

        var expiration = DateTime.UtcNow.AddSeconds(deviceResponse.ExpiresIn ?? 120);
        var interval = TimeSpan.FromSeconds(deviceResponse.Interval);

        TokenResponse? tokenResponse = null;

        while (DateTime.UtcNow < expiration)
        {
            await Task.Delay(interval);

            tokenResponse = await client.RequestDeviceTokenAsync(new DeviceTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = _options.ClientId,
                ClientCredentialStyle = ClientCredentialStyle.PostBody,
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
                        if (deviceResponse.Raw is not null)
                        {
                            AnsiConsole.WriteLine(deviceResponse.Raw);
                        }
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

        if (cachedToken.ExpiresAt > DateTime.UtcNow.AddMinutes(1))
        {
            return cachedToken.AccessToken;
        }

        if (string.IsNullOrEmpty(cachedToken.RefreshToken)) return null;

        var client = clientFactory.CreateClient();
        var disco = await GetDiscoveryDocumentAsync(client);
        if (disco is null) return null;
        
        #if DEBUG
        AnsiConsole.WriteLine("🔄 Refreshing access token...");
        #endif 
        
        var tokenResponse = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = _options.ClientId,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
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
            Address = _options.Authority,
            Policy = new DiscoveryPolicy
            {
                RequireHttps = _options.RequireHttps,
                ValidateEndpoints = false,
                ValidateIssuerName = true
            }
        });

        if (disco.IsError)
        {
            AnsiConsole.WriteLine($"❌ Discovery failed: {disco.Error}");
            return null;
        }
        
        #if DEBUG
        AnsiConsole.WriteLine($"✅ Token endpoint: {disco.TokenEndpoint}");
        AnsiConsole.WriteLine($"✅ Device auth endpoint: {disco.DeviceAuthorizationEndpoint}");
        #endif
        
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
