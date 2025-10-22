namespace Amolenk.Admitto.Cli.Commands.Events;

public static class EventFormatHelper
{
    public static string GetStatusString(
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        DateTimeOffset? registrationOpensAt,
        DateTimeOffset? registrationEndsAt)
    {
        if (DateTimeOffset.UtcNow < registrationOpensAt)
        {
            return $"Upcoming (registration opens {registrationOpensAt.Humanize()})";
        }

        if (DateTimeOffset.UtcNow < registrationEndsAt)
        {
            return $"[green]Registration Open (registration closes {registrationEndsAt.Humanize()})[/]";
        }

        if (DateTimeOffset.UtcNow < startsAt)
        {
            if (registrationOpensAt is null || registrationEndsAt is null)
            {
                return $"Upcoming (registration window tbd)";
            }
            
            return $"Registration Closed (event starts {startsAt.Humanize()})";
        }

        if (DateTimeOffset.UtcNow < endsAt)
        {
            return $"[blue]Ongoing (event ends {endsAt.Humanize()})[/]";
        }

        return $"[red]Past (event ended {endsAt.Humanize()})[/]";
    }
}