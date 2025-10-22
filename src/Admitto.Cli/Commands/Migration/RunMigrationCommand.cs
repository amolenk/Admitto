using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Migration;

public class RunMigrationSettings : CommandSettings
{
    [CommandOption("-n|--name")]
    [Description("The name of the migration to run")]
    public string? Name { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return ValidationErrors.MigrationNameMissing;
        }

        return base.Validate();
    }
}

public class RunMigrationCommand(IApiService apiService)
    : AsyncCommand<RunMigrationSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RunMigrationSettings settings)
    {
        var result = await apiService.CallApiAsync(async client => await client.Migration[settings.Name!].PostAsync());
        if (result is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully ran migration '{settings.Name}'.");
        return 0;
    }
}