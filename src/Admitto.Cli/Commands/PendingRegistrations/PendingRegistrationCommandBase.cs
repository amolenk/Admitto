using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.PendingRegistrations;

public class PendingRegistrationSettings : CommandSettings
{
    [CommandOption("-t|--team")] 
    public required string TeamSlug { get; set; }

    [CommandOption("-e|--event")] 
    public required string EventSlug { get; set; }
}

public abstract class PendingRegistrationCommandBase<TSettings> : ApiCommand<TSettings>
    where TSettings : PendingRegistrationSettings
{
    protected PendingRegistrationCommandBase(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
        : base(accessTokenProvider, configuration)
    {
    }
}