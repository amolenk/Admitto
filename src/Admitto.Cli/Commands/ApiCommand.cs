using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Commands.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Amolenk.Admitto.Cli.Commands;

public abstract class ApiCommand<TSettings>(IAccessTokenProvider accessTokenProvider, IConfiguration configuration) 
    : AsyncCommand<TSettings> where TSettings : CommandSettings
{
    protected string GetTeamSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            value = configuration[ConfigSettings.DefaultTeamSetting];
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Team slug is required.");
        }

        return value;
    }

    protected string GetEventSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            value = configuration[ConfigSettings.DefaultEventSetting];
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Event slug is required.");
        }

        return value;
    }
    
    protected string GetInput(Func<string> getValue, string description)
    {
        var value = getValue();
        if (string.IsNullOrWhiteSpace(value))
        {
            value = AnsiConsole.Prompt(new TextPrompt<string>($"{description}?"));
        }
        return value.Trim();
    }

    protected DateTimeOffset GetDateTimeOffsetInput(Func<DateTimeOffset?> getValue, string description)
    {
        var value = getValue();
        return value ?? AnsiConsole.Prompt(new TextPrompt<DateTimeOffset>($"{description}?"));
    }

    protected static List<TItem> Parse<TItem>(string[]? input, Func<string, string, TItem> createItem)
    {
        return Parse<TItem, string>(input, createItem);
    }

    protected static List<TItem> Parse<TItem, TValue>(string[]? input, Func<string, TValue, TItem> createItem)
    {
        var result = new List<TItem>();        
        
        foreach (var item in input ?? [])
        {
            var parts = item.Split('=', 2);
            if (parts.Length == 2)
            {
                var typedValue = (TValue)Parse<TValue>(parts[1]);
                
                result.Add(createItem(parts[0], typedValue));
            }
            else
            {
                throw new Exception($"Invalid format '{item}'. Expected format is 'key=value'.");
            }
        }

        return result;
    }

    private static object Parse<T>(string value)
    {
        if (typeof(T) == typeof(Guid))
        {
            if (Guid.TryParse(value, out var guid))
            {
                return guid;
            }

            throw new ArgumentException($"'{value}' is not a valid GUID.");
        }

        return Convert.ChangeType(value, typeof(T));
    }
    
    protected async ValueTask<bool> CallApiAsync(Func<ApiClient, ValueTask> callApi)
    {
        try
        {
            var apiClient = GetApiClient();
            await callApi(apiClient);
            return true;
        }
        catch (ProblemDetails e)
        {
            WriteException(e);
            return false;
        }
        catch (HttpValidationProblemDetails e)
        {
            WriteException(e);
            return false;
        }
        catch (Exception e)
        {
            WriteException(e);
            return false;
        }
    }
    
    protected async ValueTask<TResponse?> CallApiAsync<TResponse>(Func<ApiClient, ValueTask<TResponse>> callApi)
    {
        try
        {
            var apiClient = GetApiClient();
            return await callApi(apiClient);
        }
        catch (ProblemDetails e)
        {
            WriteException(e);
        }
        catch (HttpValidationProblemDetails e)
        {
            WriteException(e);
        }
        catch (Exception e)
        {
            WriteException(e);
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

    private static void WriteException(Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]{ex.Message.EscapeMarkup()}[/]");
    }
    
    private static void WriteException(ProblemDetails ex)
    {
        AnsiConsole.MarkupLine(
            !string.IsNullOrWhiteSpace(ex.Detail)
                ? $"[red]{ex.Title.EscapeMarkup()}:[/] {ex.Detail.EscapeMarkup()}"
                : $"[red]{ex.Title.EscapeMarkup()}[/]");
    }
    
    private static void WriteException(HttpValidationProblemDetails ex)
    {
        AnsiConsole.MarkupLine(
            !string.IsNullOrWhiteSpace(ex.Detail)
                ? $"[red]{ex.Title.EscapeMarkup()}:[/] {ex.Detail.EscapeMarkup()}"
                : $"[red]Error: {ex.Title.EscapeMarkup()}[/]");

        if (ex.Errors is null) return;
        
        // https://github.com/microsoft/kiota/issues/62
        foreach (var error in ex.Errors.AdditionalData)
        {
            foreach (var errorValue in ((UntypedArray)error.Value).GetValue())
            {
                AnsiConsole.WriteLine(
                    $"- {((UntypedString)errorValue).GetValue().EscapeMarkup()}");
            }
        }
    }
}