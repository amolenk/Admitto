using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Common.Auth;

namespace Amolenk.Admitto.Cli.Commands.Auth;

public class LogoutSettings : CommandSettings
{
}

public class LogoutCommand(IAuthService authService) : Command<LogoutSettings>
{
    public override int Execute(CommandContext context, LogoutSettings settings)
    {
        authService.Logout();
        
        AnsiConsoleExt.WriteSuccesMessage("Successfully logged out.");
        return 0;
    }
}