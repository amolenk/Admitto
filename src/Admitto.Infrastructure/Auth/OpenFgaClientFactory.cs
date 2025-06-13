using OpenFga.Sdk.Client;

namespace Amolenk.Admitto.Infrastructure.Auth;

/// <summary>
/// Creates an OpenFgaClient. Tries to use the store ID and authorization model ID from the configuration.
/// If they are not set, it will try to retrieve them from the OpenFGA server.
/// </summary>
/// <remarks>
/// In the production scenario, we get the store ID and authorization model ID from configuration.
/// For dev/test, the migration project will create the store and authorization model if they do not exist. That
/// will result in new store and authorization model IDs, which are non-deterministic. To allow for a smooth
/// development experience, we implemented 'auto-discovery' (using the OpenFGA SDK to retrieve the store ID by name and
/// the authorization model ID of the latest model).
///
/// We use the injected HttpClient, so there's no need to Dispose of the OpenFgaClient.
/// </remarks>
public class OpenFgaClientFactory(HttpClient httpClient, string? storeId = null, string? authorizationModelId = null)
{
    public const string StoreName = "Admitto";

    private readonly ClientConfiguration _configuration = new()
    {
        ApiUrl = httpClient.BaseAddress!.ToString().TrimEnd('/'),
        StoreId = storeId,
        AuthorizationModelId = authorizationModelId
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
                _configuration.StoreId = await client.TryGetStoreIdAsync();
            }

            if (string.IsNullOrEmpty(_configuration.AuthorizationModelId)
                && !string.IsNullOrEmpty(_configuration.StoreId))
            {
                _configuration.AuthorizationModelId = await client.TryGetAuthorizationModelIdAsync();
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
