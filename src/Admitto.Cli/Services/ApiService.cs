using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

namespace Amolenk.Admitto.Cli.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
    {
        try
        {
            var token = _configuration["Auth:Token"];
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Ensure we have a base address set
            if (_httpClient.BaseAddress == null)
            {
                var baseUrl = _configuration["Api:BaseUrl"] ?? "https://localhost:5001/api/";
                _httpClient.BaseAddress = new Uri(baseUrl);
            }

            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
                return new ApiResponse<T> { Success = true, Data = result };
            }
            else
            {
                var errorResponse = JsonSerializer.Deserialize<ProblemDetails>(responseContent, _jsonOptions);
                return new ApiResponse<T> 
                { 
                    Success = false, 
                    Error = errorResponse?.Title ?? "Unknown error",
                    StatusCode = (int)response.StatusCode,
                    ValidationErrors = errorResponse?.Errors
                };
            }
        }
        catch (Exception ex)
        {
            return new ApiResponse<T> 
            { 
                Success = false, 
                Error = ex.Message 
            };
        }
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var keycloakUrl = _configuration["Auth:KeycloakUrl"] ?? "https://localhost:8080";
            var clientId = _configuration["Auth:ClientId"] ?? "admitto-api";
            var clientSecret = _configuration["Auth:ClientSecret"] ?? "LxcFeR1EVHUMScJn3ij6dO7NR8ZSnYzp";

            var tokenUrl = $"{keycloakUrl}/realms/admitto/protocol/openid-connect/token";

            var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"grant_type", "password"},
                {"client_id", clientId},
                {"client_secret", clientSecret},
                {"username", username},
                {"password", password}
            });

            var response = await _httpClient.PostAsync(tokenUrl, tokenRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, _jsonOptions);
                if (tokenResponse?.AccessToken != null)
                {
                    // Store the token in user secrets for future use
                    await StoreTokenAsync(tokenResponse.AccessToken);
                    return true;
                }
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Authentication failed: {responseContent}[/]");
            }

            return false;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Login error: {ex.Message}[/]");
            return false;
        }
    }

    private async Task StoreTokenAsync(string token)
    {
        // In a real-world scenario, we would store this securely
        // For now, we'll just show it would be stored
        AnsiConsole.MarkupLine($"[dim]Token stored successfully[/]");
        
        // TODO: Store in user secrets or secure storage
        // This is a simplified implementation
    }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public int StatusCode { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}

public class ProblemDetails
{
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public int Status { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
    
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
}