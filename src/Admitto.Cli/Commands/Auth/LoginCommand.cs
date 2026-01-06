using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Common.Auth;

namespace Amolenk.Admitto.Cli.Commands.Auth;

public class LoginSettings : CommandSettings
{
}

public class LoginCommand(IAuthService authService) : AsyncCommand<LoginSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, LoginSettings settings, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync();
        if (result)
        {
            AnsiConsoleExt.WriteSuccesMessage("Successfully logged in.");
            return 0;
        }

        AnsiConsoleExt.WriteErrorMessage("Login failed.");
        return 1;
    }
}