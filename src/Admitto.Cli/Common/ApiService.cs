using Amolenk.Admitto.Cli.Api;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Amolenk.Admitto.Cli.Common;

public interface IApiService
{
    ValueTask<bool> CallApiAsync(Func<ApiClient, ValueTask> callApi);

    ValueTask<TResponse?> CallApiAsync<TResponse>(Func<ApiClient, ValueTask<TResponse>> callApi);
}

public class ApiService(IOptions<AdmittoOptions> options, IAccessTokenProvider accessTokenProvider)
    : IApiService
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
            AnsiConsoleExt.WriteException(e);
            return false;
        }
        catch (HttpValidationProblemDetails e)
        {
            AnsiConsoleExt.WriteException(e);
            return false;
        }
        catch (Exception e)
        {
            AnsiConsoleExt.WriteException(e);
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
            AnsiConsoleExt.WriteException(e);
        }
        catch (HttpValidationProblemDetails e)
        {
            AnsiConsoleExt.WriteException(e);
        }
        catch (Exception e)
        {
            AnsiConsoleExt.WriteException(e);
        }

        return default;
    }

    private ApiClient GetApiClient()
    {
        var endpoint = options.Value.Endpoint;
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