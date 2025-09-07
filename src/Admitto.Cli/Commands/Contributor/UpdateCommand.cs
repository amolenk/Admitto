using Amolenk.Admitto.Cli.Commands.Events;
using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Contributor;

public class UpdateSettings : TeamEventSettings
{
    [CommandOption("--id")] 
    public Guid? Id { get; set; }

    [CommandOption("--firstName")]
    public string? FirstName { get; set; } = null!;

    [CommandOption("--lastName")]
    public string? LastName { get; set; } = null!;

    [CommandOption("--additionalDetail")]
    public string[]? AdditionalDetails { get; set; } = null!;

    [CommandOption("--role")]
    public ContributorRole?[]? Roles { get; set; } = null!;

    public override ValidationResult Validate()
    {
        if (Id is null)
        {
            return ValidationErrors.IdMissing;
        }

        return base.Validate();
    }
}

public class UpdateCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : EventCommandBase<UpdateSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpdateSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var request = new UpdateContributorRequest
        {
            FirstName = settings.FirstName,
            LastName = settings.LastName,
            // TODO Test
            AdditionalDetails = Parse<AdditionalDetailDto>(
                settings.AdditionalDetails,
                (name, value) => new AdditionalDetailDto
                {
                    Name = name,
                    Value = value
                }),
            Roles = settings.Roles?.ToList()
        };

        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Contributors[settings.Id!.Value].PatchAsync(request));
        if (response is null) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully updated contributor.[/]");
        return 0;
    }
}