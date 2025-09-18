using Amolenk.Admitto.Cli.Commands.Events;

namespace Amolenk.Admitto.Cli.Commands.Email.Bulk;

public class RemoveSettings : TeamEventSettings
{
    [CommandOption("--id")] public Guid? Id { get; set; }

    public override ValidationResult Validate()
    {
        if (Id is null)
        {
            return ValidationErrors.IdMissing;
        }

        return base.Validate();
    }
}

public class RemoveCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : EventCommandBase<RemoveSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, RemoveSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Emails.Bulk[settings.Id!.Value].DeleteAsync());
        if (response is null) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully removed bulk email.[/]");
        return 0;
    }
}