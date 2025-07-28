namespace Amolenk.Admitto.Cli.Commands;

public class PagingTeamSettings : PagingSettings
{
    [CommandOption("-t|--team")]
    public required string TeamSlug { get; set; }
}