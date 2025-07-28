using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Email.Test;

public class PreviewEmailSettings : TeamEventSettings
{
    [CommandOption("--type")]
    [EmailTypeDescription]
    public required EmailType EmailType { get; init; }

    [CommandOption("--entityId")]
    public required Guid DataEntityId { get; init; }
    
    [CommandOption("--output")]
    public required string OutputPath { get; init; }
}

public class PreviewEmailCommand(
    IAccessTokenProvider accessTokenProvider,
    IConfiguration configuration)
    : ApiCommand<PreviewEmailSettings>(accessTokenProvider, configuration)
{
    public sealed override async Task<int> ExecuteAsync(CommandContext context, PreviewEmailSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var request = new PreviewEmailRequest
        {
            DataEntityId = settings.DataEntityId
        };
  
        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Emails[settings.EmailType.ToString()].Preview.PostAsync(request));
        if (response is null) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully generated '{settings.EmailType}' preview with subject '{response.Subject}'.[/]");
        
        if (!string.IsNullOrWhiteSpace(settings.OutputPath))
        {
            var directory = Path.GetDirectoryName(settings.OutputPath)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            await File.WriteAllTextAsync(settings.OutputPath, response.Body!);
        }
        else
        {
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine(response.Body!);
        }
        
        return 0;
    }
}

