namespace Amolenk.Admitto.Cli.Commands.Migration;

public class RunMigrationSettings : CommandSettings
{
    [CommandOption("-n|--name")]
    [Description("The name of the migration to run.")]
    public string? Name { get; init; }
}

public class RunMigrationCommand(InputService inputService, OutputService outputService, IApiService apiService)
    : AsyncCommand<RunMigrationSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RunMigrationSettings settings)
    {
        var migrationName = settings.Name ?? await GetMigrationName(apiService);

        var result = await apiService.CallApiAsync(async client => await client.Migration[migrationName].PostAsync());
        if (result is null) return 1;

        outputService.WriteSuccesMessage($"Successfully ran migration '{migrationName}'.");
        return 0;
    }

    private async ValueTask<string> GetMigrationName(IApiService apiService)
    {
        var response = await apiService.CallApiAsync(async client => await client.Migration.GetAsync());
        if (response?.Migrations?.Count > 0)
        {
            return inputService.GetStringFromList("Migration name", response.Migrations);
        }

        throw new InvalidOperationException("No migrations available.");
    }
}