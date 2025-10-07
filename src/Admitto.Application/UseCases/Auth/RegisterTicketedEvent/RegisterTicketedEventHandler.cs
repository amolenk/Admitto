namespace Amolenk.Admitto.Application.UseCases.Auth.RegisterTicketedEvent;

/// <summary>
/// Registers a ticketed event with the authorization service.
/// </summary>
public class RegisterTicketedEventHandler(
    IAuthorizationService authorizationService,
    ILogger<RegisterTicketedEventHandler> logger)
    : ICommandHandler<RegisterTicketedEventCommand>
{
    public async ValueTask HandleAsync(RegisterTicketedEventCommand command, CancellationToken cancellationToken)
    {
        await authorizationService.AddTicketedEventAsync(command.TeamId, command.TicketedEventId, cancellationToken);
        
        logger.LogInformation(
            "Registered ticketed event '{ticketedEvent}' for team '{team}' with the authorization service.",
            command.TicketedEventId,
            command.TeamId);
    }
}