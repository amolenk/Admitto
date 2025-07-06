using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using Amolenk.Admitto.Cli.Services;

namespace Amolenk.Admitto.Cli.Commands;

public class LoginSettings : CommandSettings
{
    [CommandOption("-i|--interactive")]
    [Description("Force interactive login (opens browser)")]
    public bool Interactive { get; set; }
}

public class LoginCommand : AsyncCommand<LoginSettings>
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public LoginCommand(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, LoginSettings settings)
    {
        AnsiConsole.MarkupLine("[bold blue]Admitto CLI Login[/]");
        AnsiConsole.WriteLine();

        // Attempt login using OAuth2 authorization code flow with PKCE
        var loginResult = await AnsiConsole.Status()
            .StartAsync("Authenticating...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Star);
                return await _authService.LoginAsync();
            });

        if (!string.IsNullOrEmpty(loginResult))
        {
            AnsiConsole.MarkupLine("[green]✓ Successfully authenticated![/]");
            return 0;
        }
        else
        {
            AnsiConsole.MarkupLine("[red]✗ Authentication failed.[/]");
            return 1;
        }
    }
}