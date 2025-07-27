using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Infrastructure.UserManagement.Keycloak;

public class KeycloakAccessTokenHandler(IOptions<KeycloakOptions> options, string? tokenProviderBaseUrl = null) : DelegatingHandler
{
    private readonly KeycloakOptions _options = options.Value;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private TokenInfo? _tokenInfo;
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        // Don't add auth header to token requests to avoid infinite recursion
        if (IsTokenEndpointRequest(request))
        {
            return await base.SendAsync(request, cancellationToken);
        }
        
        // Apply token to request
        var token = await GetAccessTokenAsync(request);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized) return response;
        
        // Token might be invalid - force refresh and try again
        token = await GetAccessTokenAsync(request, forceRefresh: true);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        response = await base.SendAsync(request, cancellationToken);

        return response;
    }
    
    private static bool IsTokenEndpointRequest(HttpRequestMessage request)
    {
        return request.RequestUri?.PathAndQuery.Contains("/protocol/openid-connect/token") == true;
    }
    
    private async Task<string> GetAccessTokenAsync(HttpRequestMessage request, bool forceRefresh = false)
    {
        if (!forceRefresh && _tokenInfo?.IsValid == true)
        {
            return _tokenInfo.AccessToken;
        }

        await _semaphore.WaitAsync();
        try
        {
            // Double-check token validity after acquiring the semaphore
            if (!forceRefresh && _tokenInfo?.IsValid == true)
            {
                return _tokenInfo.AccessToken;
            }

            var baseUrl = tokenProviderBaseUrl?.TrimEnd('/')
                          ?? request.RequestUri?.GetLeftPart(UriPartial.Authority);

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, 
                new Uri(baseUrl + _options.TokenPath));
            
            tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", _options.ClientId },
                { "grant_type", "password" },
                { "username", _options.Username },
                { "password", _options.Password }
            });
            
            var response = await base.SendAsync(tokenRequest, CancellationToken.None);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to obtain access token: {error}", null, response.StatusCode);
            }
            
            var tokenJson = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(tokenJson);
            
            if (tokenResponse?.AccessToken == null || tokenResponse.ExpiresIn <= 0)
            {
                throw new InvalidOperationException("Invalid token response received.");
            }

            _tokenInfo = new TokenInfo(
                tokenResponse.AccessToken,
                DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 30) // Buffer for expiration
            );

            return _tokenInfo.AccessToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private class TokenInfo
    {
        public string AccessToken { get; }
        public DateTime ExpiresAt { get; }
        public bool IsValid => DateTime.UtcNow < ExpiresAt;

        public TokenInfo(string accessToken, DateTime expiresAt)
        {
            AccessToken = accessToken;
            ExpiresAt = expiresAt;
        }
    }
}

public record TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken = null);



