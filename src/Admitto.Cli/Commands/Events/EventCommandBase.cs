namespace Amolenk.Admitto.Cli.Commands.Events;

public abstract class EventCommandBase<TSettings>(
    IAccessTokenProvider accessTokenProvider,
    IConfiguration configuration,
    OutputService outputService)
    : ApiCommand<TSettings>(accessTokenProvider, configuration, outputService)
    where TSettings : TeamSettings
{
    protected static string GetStatusString(
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