using Amolenk.Admitto.Cli.Auth;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Cli.Api;

public interface IAdmittoService
{
    ValueTask<bool> SendAsync(Func<ApiClient, Task> callApi);

    ValueTask<TResponse?> QueryAsync<TResponse>(Func<ApiClient, Task<TResponse>> callApi);

    Task<Guid?> FindAttendeeAsync(string teamSlug, string eventSlug, string email);
}

public class AdmittoService(IOptions<AdmittoOptions> options, IAuthService authService)
    : IAdmittoService
{
    public async ValueTask<bool> SendAsync(Func<ApiClient, Task> callApi)
    {
        try
        {
            var apiClient = await GetApiClientAsync();
            await callApi(apiClient);
            return true;
        }
        catch (ApiException<HttpValidationProblemDetails> e)
        {
            AnsiConsoleExt.WriteException(e.Result);
            return false;
        }
        catch (ApiException<ProblemDetails> e)
        {
            AnsiConsoleExt.WriteException(e.Result);
            return false;
        }
        catch (ApiException e)
        {
            AnsiConsoleExt.WriteErrorMessage(e.Message);
            return false;
        }
        catch (Exception e)
        {
            AnsiConsoleExt.WriteException(e);
            return false;
        }
    }

    public async ValueTask<TResponse?> QueryAsync<TResponse>(Func<ApiClient, Task<TResponse>> callApi)
    {
        try
        {
            var apiClient = await GetApiClientAsync();
            return await callApi(apiClient);
        }
        catch (ApiException<ProblemDetails> e)
        {
            AnsiConsoleExt.WriteException(e.Result);
        }
        catch (ApiException<HttpValidationProblemDetails> e)
        {
            AnsiConsoleExt.WriteException(e.Result);
        }
        catch (ApiException e) when (e.StatusCode == 404)
        {
            return default;
        }
        catch (ApiException e)
        {
            AnsiConsoleExt.WriteErrorMessage(e.Message);
        }
        catch (Exception e)
        {
            AnsiConsoleExt.WriteException(e);
        }

        return default;
    }
    
    public async Task<Guid?> FindAttendeeAsync(string teamSlug, string eventSlug, string email)
    {
        var response =
            await QueryAsync<FindAttendeeResponse>(client => client.FindAttendeeAsync(teamSlug, eventSlug, email));
        
        return response?.AttendeeId;
    }

    private async ValueTask<ApiClient> GetApiClientAsync()
    {
        var endpoint = options.Value.Endpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException("API endpoint is not configured.");
        }
        
        var token = await authService.GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("Authentication required. Please login first.");
        }

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        return new ApiClient(httpClient)
        {
            BaseUrl = endpoint
        };
    }
}

