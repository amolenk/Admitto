namespace Amolenk.Admitto.Cli.Commands;

public class LoginSettings : CommandSettings
{
}

public class LoginCommand(IAuthService authService, OutputService outputService) : AsyncCommand<LoginSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, LoginSettings settings)
    {
        var result = await authService.LoginAsync();
        if (result)
        {
            outputService.WriteSuccesMessage("Successfully logged in.");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]✗ Login failed.[/]");
        return 1;
    }
}