using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Migration;

public class ListMigrationsCommand(IApiService apiService) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var result = await apiService.CallApiAsync(async client => await client.Migration.GetAsync());
        if (result is null) return 1;
        
        foreach (var migration in result.Migrations ?? [])
        {
            AnsiConsoleExt.WriteSuccesMessage(migration);
        }

        return 0;
    }
}