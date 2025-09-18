using Amolenk.Admitto.Cli.Api;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Amolenk.Admitto.Cli.Services;

public interface IApiService
{
    ValueTask<bool> CallApiAsync(Func<ApiClient, ValueTask> callApi);

    ValueTask<TResponse?> CallApiAsync<TResponse>(Func<ApiClient, ValueTask<TResponse>> callApi);
}

public class ApiService(
    IAccessTokenProvider accessTokenProvider,
    OutputService outputService,
    IConfiguration configuration) : IApiService
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
            outputService.WriteException(e);
            return false;
        }
        catch (HttpValidationProblemDetails e)
        {
            outputService.WriteException(e);
            return false;
        }
        catch (Exception e)
        {
            outputService.WriteException(e);
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
            outputService.WriteException(e);
        }
        catch (HttpValidationProblemDetails e)
        {
            outputService.WriteException(e);
        }
        catch (Exception e)
        {
            outputService.WriteException(e);
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