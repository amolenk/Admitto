using Amolenk.Admitto.Cli.Commands.Teams;

namespace Amolenk.Admitto.Cli.Commands;

public class TeamEventSettings : TeamSettings
{
    [CommandOption("-e|--event")]
    public required string EventSlug { get; init; }
}