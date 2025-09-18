namespace Amolenk.Admitto.Cli.Commands;

public class TeamEventSettings : TeamSettings
{
    [CommandOption("-e|--event")]
    [Description("The event slug. If not provided, the default team will be used if set")]
    public string? EventSlug { get; set; }
}