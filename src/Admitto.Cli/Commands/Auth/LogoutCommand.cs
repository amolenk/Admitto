using Spectre.Console;

namespace Amolenk.Admitto.Cli.Commands;

public class LogoutSettings : CommandSettings
{
}

public class LogoutCommand(IAuthService authService, OutputService outputService) : Command<LogoutSettings>
{
    public override int Execute(CommandContext context, LogoutSettings settings)
    {
        authService.Logout();
        
        outputService.WriteSuccesMessage("Successfully logged out.");
        return 0;
    }
}