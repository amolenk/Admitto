using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using Amolenk.Admitto.Cli.Services;

namespace Amolenk.Admitto.Cli.Commands;

public class LoginSettings : CommandSettings
{
    [CommandOption("-u|--username")]
    [Description("Username for authentication")]
    public string? Username { get; set; }

    [CommandOption("-p|--password")]
    [Description("Password for authentication")]
    public string? Password { get; set; }
}

public class LoginCommand : AsyncCommand<LoginSettings>
{
    private readonly ApiService _apiService;
    private readonly IConfiguration _configuration;

    public LoginCommand(ApiService apiService, IConfiguration configuration)
    {
        _apiService = apiService;
        _configuration = configuration;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, LoginSettings settings)
    {
        AnsiConsole.MarkupLine("[bold blue]Admitto CLI Login[/]");
        AnsiConsole.WriteLine();

        // Get username if not provided
        var username = settings.Username;
        if (string.IsNullOrEmpty(username))
        {
            username = AnsiConsole.Ask<string>("Enter your [green]username[/]:");
        }

        // Get password if not provided
        var password = settings.Password;
        if (string.IsNullOrEmpty(password))
        {
            password = AnsiConsole.Prompt(
                new TextPrompt<string>("Enter your [green]password[/]:")
                    .Secret());
        }

        // Attempt login
        var loginResult = await AnsiConsole.Status()
            .StartAsync("Logging in...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Star);
                return await _apiService.LoginAsync(username, password);
            });

        if (loginResult)
        {
            AnsiConsole.MarkupLine("[green]✓ Successfully logged in![/]");
            return 0;
        }
        else
        {
            AnsiConsole.MarkupLine("[red]✗ Login failed. Please check your credentials.[/]");
            return 1;
        }
    }
}