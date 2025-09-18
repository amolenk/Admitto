namespace Amolenk.Admitto.Cli.Commands.Events;

public abstract class EventCommandBase<TSettings>(
    IAccessTokenProvider accessTokenProvider,
    IConfiguration configuration)
    : ApiCommand<TSettings>(accessTokenProvider, configuration)
    where TSettings : TeamSettings
{
    protected static string GetStatusString(DateTimeOffset registrationOpensAt, DateTimeOffset registrationEndsAt, 
        DateTimeOffset startsAt, DateTimeOffset endsAt)
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
            return $"Registration Closed (event starts {startsAt.Humanize()})";
        }
        
        if (DateTimeOffset.UtcNow < endsAt)
        {
            return $"[blue]Ongoing (event ends {endsAt.Humanize()})[/]";
        }
        
        return $"[red]Past (event ended {endsAt.Humanize()})[/]";
    }
}