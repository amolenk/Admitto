namespace Amolenk.Admitto.Cli.Commands;

public class TeamSettings : CommandSettings
{
    [CommandOption("-t|--team")]
    [Description("The team slug. If not provided, the default team will be used if set")]
    public string? TeamSlug { get; set; }
}