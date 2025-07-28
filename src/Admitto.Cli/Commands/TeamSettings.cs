namespace Amolenk.Admitto.Cli.Commands;

public class TeamSettings : CommandSettings
{
    [CommandOption("-t|--team")]
    public required string TeamSlug { get; init; }
}