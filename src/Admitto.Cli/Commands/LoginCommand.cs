using Spectre.Console;

namespace Amolenk.Admitto.Cli.Commands;

public class LoginSettings : CommandSettings
{
}

public class LoginCommand(IAuthService authService) : AsyncCommand<LoginSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, LoginSettings settings)
    {
        var result = await authService.LoginAsync();
        if (result)
        {
            AnsiConsole.MarkupLine("[green]✓ Successfully logged in.[/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]✗ Login failed.[/]");
        return 1;
    }
}