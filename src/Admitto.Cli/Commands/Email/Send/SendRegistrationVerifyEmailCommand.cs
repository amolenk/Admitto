using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Email.Send;

public class SendRegistrationEmailSettings : TeamEventSettings
{
    [CommandOption("--registrationId")] 
    public required Guid RegistrationId { get; init; }

    public override ValidationResult Validate()
    {
        if (RegistrationId == Guid.Empty)
        {
            return ValidationResult.Error("Registration ID must be specified and cannot be empty.");
        }
        
        return base.Validate();
    }
}

public class SendRegistrationVerifyEmailCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : SendEmailCommandBase<SendRegistrationEmailSettings>(EmailType.VerifyRegistration, accessTokenProvider, configuration)
{
    protected override async ValueTask<(Guid DataEntityId, string Email)> GetEntityInfoAsync(
        SendRegistrationEmailSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);
        
        var response = await CallApiAsync(async client => 
            await client.Teams[teamSlug].Events[eventSlug].Registrations[settings.RegistrationId] .GetAsync());
        
        if (response is null)
        {
            throw new InvalidOperationException(
                $"Failed to retrieve pending registration with ID {settings.RegistrationId}.");
        }

        if (response.Status != RegistrationRequestStatus.Unverified)
        {
            throw new InvalidOperationException("Only unverified registrations can receive verification emails.");
        }

        return (settings.RegistrationId, response.Email!);
    }
}
