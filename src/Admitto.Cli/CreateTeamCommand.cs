using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Amolenk.Admitto.Cli.Services;
using Amolenk.Admitto.Cli.Models;

namespace Amolenk.Admitto.Cli;

public class CreateTeamSettings : CommandSettings
{
    [CommandOption("-n|--name")]
    [Description("Name of the team")]
    public string? Name { get; set; }

    [CommandOption("-e|--sender-email")]
    [Description("Sender email for the team")]
    public string? SenderEmail { get; set; }

    [CommandOption("-s|--smtp-server")]
    [Description("SMTP server for email sending")]
    public string? SmtpServer { get; set; }

    [CommandOption("-p|--smtp-port")]
    [Description("SMTP port for email sending")]
    public int SmtpPort { get; set; } = 587;

    [CommandOption("-m|--member")]
    [Description("Team member in format 'email:role' (can be specified multiple times)")]
    public string[]? Members { get; set; }
}

public class CreateTeamCommand : AsyncCommand<CreateTeamSettings>
{
    private readonly ApiService _apiService;

    public CreateTeamCommand(ApiService apiService)
    {
        _apiService = apiService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CreateTeamSettings settings)
    {
        AnsiConsole.MarkupLine("[bold blue]Create Team[/]");
        AnsiConsole.WriteLine();

        // Get team name if not provided
        var teamName = settings.Name;
        if (string.IsNullOrEmpty(teamName))
        {
            teamName = AnsiConsole.Ask<string>("Enter the [green]team name[/]:");
        }

        // Get sender email if not provided
        var senderEmail = settings.SenderEmail;
        if (string.IsNullOrEmpty(senderEmail))
        {
            senderEmail = AnsiConsole.Ask<string>("Enter the [green]sender email[/]:");
        }

        // Get SMTP server if not provided
        var smtpServer = settings.SmtpServer;
        if (string.IsNullOrEmpty(smtpServer))
        {
            smtpServer = AnsiConsole.Ask<string>("Enter the [green]SMTP server[/]:");
        }

        // Get SMTP port if not provided or if it's the default
        var smtpPort = settings.SmtpPort;
        if (smtpPort == 587 && string.IsNullOrEmpty(settings.SmtpServer))
        {
            smtpPort = AnsiConsole.Ask("Enter the [green]SMTP port[/]:", 587);
        }

        // Get team members
        var members = new List<TeamMemberDto>();
        if (settings.Members != null && settings.Members.Length > 0)
        {
            foreach (var member in settings.Members)
            {
                if (TryParseMember(member, out var email, out var role))
                {
                    members.Add(new TeamMemberDto(email, role));
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Invalid member format: {member}. Expected format: email:role[/]");
                    return 1;
                }
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]No members specified. You can add members later.[/]");
        }

        // Create the request
        var request = new CreateTeamRequest(
            teamName,
            new EmailSettingsDto(senderEmail, smtpServer, smtpPort),
            members
        );

        // Call the API
        var response = await AnsiConsole.Status()
            .StartAsync("Creating team...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Star);
                return await _apiService.PostAsync<CreateTeamResponse>("teams", request);
            });

        if (response.Success)
        {
            AnsiConsole.MarkupLine($"[green]✓ Team '{teamName}' created successfully![/]");
            AnsiConsole.MarkupLine($"[dim]Team ID: {response.Data?.Id}[/]");
            return 0;
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗ Failed to create team: {response.Error}[/]");
            
            if (response.ValidationErrors != null && response.ValidationErrors.Count > 0)
            {
                AnsiConsole.MarkupLine("[red]Validation errors:[/]");
                foreach (var error in response.ValidationErrors)
                {
                    AnsiConsole.MarkupLine($"[red]- {error.Key}: {string.Join(", ", error.Value)}[/]");
                }
            }
            
            return 1;
        }
    }

    private static bool TryParseMember(string member, out string email, out string role)
    {
        email = "";
        role = "";
        
        var parts = member.Split(':');
        if (parts.Length == 2)
        {
            email = parts[0].Trim();
            role = parts[1].Trim();
            return !string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(role);
        }
        
        return false;
    }
}