using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using Amolenk.Admitto.Cli.Services;

namespace Amolenk.Admitto.Cli.Commands;

public class LogoutSettings : CommandSettings
{
}

public class LogoutCommand : AsyncCommand<LogoutSettings>
{
    private readonly IAuthService _authService;

    public LogoutCommand(IAuthService authService)
    {
        _authService = authService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, LogoutSettings settings)
    {
        AnsiConsole.MarkupLine("[bold blue]Admitto CLI Logout[/]");
        AnsiConsole.WriteLine();

        var logoutResult = await AnsiConsole.Status()
            .StartAsync("Logging out...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Star);
                return await _authService.LogoutAsync();
            });

        return logoutResult ? 0 : 1;
    }
}