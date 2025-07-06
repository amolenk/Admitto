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
    private readonly IAuthService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiService(HttpClient httpClient, IConfiguration configuration, IAuthService authService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _authService = authService;
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
            var token = await _authService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                return new ApiResponse<T> 
                { 
                    Success = false, 
                    Error = "Authentication required. Please login first." 
                };
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