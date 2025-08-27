using Humanizer;
using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Events;

public abstract class EventCommandBase<TSettings>(
    IAccessTokenProvider accessTokenProvider,
    IConfiguration configuration)
    : ApiCommand<TSettings>(accessTokenProvider, configuration)
    where TSettings : TeamSettings
{
    protected static string GetStatusString(DateTimeOffset registrationStartTime, DateTimeOffset registrationEndTime, 
        DateTimeOffset startTime, DateTimeOffset endTime)
    {
        if (DateTimeOffset.UtcNow < registrationStartTime)
        {
            return $"Upcoming (registration opens {registrationStartTime.Humanize()})";
        }
        
        if (DateTimeOffset.UtcNow < registrationEndTime)
        {
            return $"[green]Registration Open (registration closes {registrationEndTime.Humanize()})[/]";
        }
        
        if (DateTimeOffset.UtcNow < startTime)
        {
            return "Registration Closed (event starts {startTime.Humanize()})";
        }
        
        if (DateTimeOffset.UtcNow < endTime)
        {
            return "[blue]Ongoing (event ends {endTime.Humanize()})[/]";
        }
        
        return "[red]Past (event ended {endTime.Humanize()})[/]";
    }
}