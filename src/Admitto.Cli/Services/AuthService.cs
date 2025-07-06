using Microsoft.Identity.Client;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Amolenk.Admitto.Cli.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IPublicClientApplication _app;
    private readonly string[] _scopes;

    public AuthService(IConfiguration configuration)
    {
        _configuration = configuration;
        
        var keycloakUrl = _configuration["Auth:KeycloakUrl"] ?? "https://localhost:8080";
        var clientId = _configuration["Auth:ClientId"] ?? "admitto-cli";
        var tenantId = _configuration["Auth:TenantId"] ?? "admitto";
        
        _scopes = new[] { "openid", "profile", "email" };
        
        // Configure MSAL for Keycloak
        var authority = $"{keycloakUrl}/realms/{tenantId}";
        
        _app = PublicClientApplicationBuilder
            .Create(clientId)
            .WithAuthority(authority)
            .WithDefaultRedirectUri()
            .Build();
    }

    public async Task<string?> LoginAsync()
    {
        try
        {
            // First, try to get a token silently from the cache
            var accounts = await _app.GetAccountsAsync();
            if (accounts.Any())
            {
                try
                {
                    var result = await _app.AcquireTokenSilent(_scopes, accounts.First())
                        .ExecuteAsync();
                    return result.AccessToken;
                }
                catch (MsalUiRequiredException)
                {
                    // Silent token acquisition failed, fall through to interactive
                }
            }

            // If we don't have a cached token, use interactive authentication
            return await LoginInteractiveAsync();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Authentication error: {ex.Message}[/]");
            return null;
        }
    }

    private async Task<string?> LoginInteractiveAsync()
    {
        AnsiConsole.MarkupLine($"[blue]Opening browser for authentication...[/]");
        AnsiConsole.MarkupLine($"[dim]If the browser doesn't open automatically, please navigate to the authentication URL manually.[/]");
        
        try
        {
            var result = await _app.AcquireTokenInteractive(_scopes)
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync();

            AnsiConsole.MarkupLine($"[green]✓ Authentication successful![/]");
            AnsiConsole.MarkupLine($"[dim]Welcome, {result.Account.Username}[/]");
            
            return result.AccessToken;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Interactive authentication failed: {ex.Message}[/]");
            return null;
        }
    }

    public async Task<bool> LogoutAsync()
    {
        try
        {
            var accounts = await _app.GetAccountsAsync();
            if (accounts.Any())
            {
                await _app.RemoveAsync(accounts.First());
                AnsiConsole.MarkupLine($"[green]✓ Logged out successfully[/]");
                return true;
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]No active session found[/]");
                return true;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Logout error: {ex.Message}[/]");
            return false;
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            var accounts = await _app.GetAccountsAsync();
            if (accounts.Any())
            {
                var result = await _app.AcquireTokenSilent(_scopes, accounts.First())
                    .ExecuteAsync();
                return result.AccessToken;
            }
        }
        catch (MsalUiRequiredException)
        {
            // Token expired or doesn't exist, need to re-authenticate
            AnsiConsole.MarkupLine($"[yellow]Session expired. Please login again.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error getting access token: {ex.Message}[/]");
        }
        
        return null;
    }

}