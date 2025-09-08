using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenFga.Sdk.Client;

namespace Amolenk.Admitto.Infrastructure.Auth.OpenFga;

/// <summary>
/// Creates an OpenFgaClient. Tries to use the latest store ID and authorization model ID from the OpenFGA server.
/// </summary>
/// <remarks>
/// We use the injected HttpClient, so there's no need to Dispose of the OpenFgaClient.
/// </remarks>
public class OpenFgaClientFactory(HttpClient httpClient, IConfiguration configuration, ILogger<OpenFgaClientFactory> logger)
{
    public const string StoreName = "Admitto";

    private readonly ClientConfiguration _configuration = new()
    {
        ApiUrl = httpClient.BaseAddress!.ToString().TrimEnd('/'),
        StoreId = configuration["OpenFGA:StoreId"],
        AuthorizationModelId = configuration["OpenFGA:AuthorizationModelId"]
    };
    
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private volatile OpenFgaClient? _client;
    
    public async ValueTask<OpenFgaClient> GetClientAsync()
    {
        // Fast path - check outside the lock
        if (_client is not null) return _client;

        // Slow path - acquire lock for initialization
        await _semaphore.WaitAsync();
        try
        {
            // Double-check after acquiring the lock
            if (_client is not null) return _client;

            var client = new OpenFgaClient(_configuration, httpClient);

            if (string.IsNullOrEmpty(_configuration.StoreId))
            {
                logger.LogWarning("Store ID not configured. Attempting to retrieve from OpenFGA server.");

                _configuration.StoreId = await client.TryGetStoreIdAsync();
            }

            if (string.IsNullOrEmpty(_configuration.AuthorizationModelId))
            {
                logger.LogWarning("Authorization Model ID not configured. Performance is better when the Authorization Model ID is configured.");
            }

            _client = client;
            return _client;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task<OpenFgaClient> UpdateStoreIdAsync(string storeId)
    {
        await _semaphore.WaitAsync();
        try
        {
            _configuration.StoreId = storeId;
            _client = null;
        }
        finally
        {
            _semaphore.Release();
        }

        return await GetClientAsync();
    }
}
