using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Commands.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Amolenk.Admitto.Cli.Services;

public interface IApiService
{
    ValueTask<bool> CallApiAsync(Func<ApiClient, ValueTask> callApi);

    ValueTask<TResponse?> CallApiAsync<TResponse>(Func<ApiClient, ValueTask<TResponse>> callApi);
}

public class ApiService(IAccessTokenProvider accessTokenProvider, IConfiguration configuration) : IApiService
{
    public async ValueTask<bool> CallApiAsync(Func<ApiClient, ValueTask> callApi)
    {
        try
        {
            var apiClient = GetApiClient();
            await callApi(apiClient);
            return true;
        }
        catch (ProblemDetails e)
        {
            OutputService.WriteException(e);
            return false;
        }
        catch (HttpValidationProblemDetails e)
        {
            OutputService.WriteException(e);
            return false;
        }
        catch (Exception e)
        {
            OutputService.WriteException(e);
            return false;
        }
    }
    
    public async ValueTask<TResponse?> CallApiAsync<TResponse>(Func<ApiClient, ValueTask<TResponse>> callApi)
    {
        try
        {
            var apiClient = GetApiClient();
            return await callApi(apiClient);
        }
        catch (ProblemDetails e)
        {
            OutputService.WriteException(e);
        }
        catch (HttpValidationProblemDetails e)
        {
            OutputService.WriteException(e);
        }
        catch (Exception e)
        {
            OutputService.WriteException(e);
        }
        
        return default;
    }
    
    private ApiClient GetApiClient()
    {
        var endpoint = configuration[ConfigSettings.EndpointSetting];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException(
                "API endpoint is not configured. Please set it using 'config set --endpoint <url>' command.");
        }
        
        var authProvider = new BaseBearerTokenAuthenticationProvider(accessTokenProvider);
        var requestAdapter = new HttpClientRequestAdapter(authProvider);
        requestAdapter.BaseUrl = endpoint;
        
        return new ApiClient(requestAdapter);
    }
}