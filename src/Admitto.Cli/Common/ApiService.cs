using Amolenk.Admitto.Cli.Api;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Amolenk.Admitto.Cli.Common;

public interface IApiService
{
    ValueTask<bool> CallApiAsync(Func<ApiClient, ValueTask> callApi);

    ValueTask<TResponse?> CallApiAsync<TResponse>(Func<ApiClient, ValueTask<TResponse>> callApi);

    Task<Guid?> FindAttendeeAsync(string teamSlug, string eventSlug, string email);
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
        catch (ApiException ex) when (ex.ResponseStatusCode == 404)
        {
            return default;
        }
        catch (Exception e)
        {
            AnsiConsoleExt.WriteException(e);
        }

        return default;
    }
    
    public async Task<Guid?> FindAttendeeAsync(string teamSlug, string eventSlug, string email)
    {
        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Attendees.ByEmail.GetAsync(config =>
            {
                config.QueryParameters.Email = email;
            }));

        return response?.AttendeeId;
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