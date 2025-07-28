using Spectre.Console;

namespace Amolenk.Admitto.Cli.Commands;

public class LogoutSettings : CommandSettings
{
}

public class LogoutCommand : Command<LogoutSettings>
{
    private readonly IAuthService _authService;

    public LogoutCommand(IAuthService authService)
    {
        _authService = authService;
    }

    public override int Execute(CommandContext context, LogoutSettings settings)
    {
        _authService.Logout();
        
        AnsiConsole.MarkupLine("[green]✓ Successfully logged out.[/]");
        return 0;
    }
}