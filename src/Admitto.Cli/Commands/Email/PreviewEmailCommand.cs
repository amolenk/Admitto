using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Email;

public class PreviewEmailSettings : TeamEventSettings
{
    [CommandOption("--emailType")]
    [EmailTypeDescription]
    public EmailType? EmailType { get; init; }

    [CommandOption("--entityId")]
    public Guid DataEntityId { get; init; }
    
    [CommandOption("--outputPath")]
    public required string OutputPath { get; init; }
    
    public override ValidationResult Validate()
    {
        if (EmailType is null)
        {
            return ValidationErrors.EmailTypeMissing;
        }

        if (DataEntityId == Guid.Empty)
        {
            return ValidationErrors.DataTypeEntityMissing;
        }

        return base.Validate();
    }
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

            if (File.Exists(settings.OutputPath) && !ConfirmOverwrite())
            {
                return 1;
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
    
    private static bool ConfirmOverwrite() =>
        AnsiConsole.Prompt(
            new TextPrompt<bool>("[red]File already exists. Overwrite (y/n)?[/]")
                .AddChoice(true)
                .AddChoice(false)
                .DefaultValue(true)
                .WithConverter(choice => choice ? "y" : "n"));
}

