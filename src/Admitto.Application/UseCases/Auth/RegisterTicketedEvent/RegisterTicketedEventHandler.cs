using Amolenk.Admitto.Application.Common;

namespace Amolenk.Admitto.Application.UseCases.Auth.RegisterTicketedEvent;

/// <summary>
/// Registers a ticketed event with the authorization service.
/// </summary>
public class RegisterTicketedEventHandler(
    IApplicationContext context,
    IAuthorizationService authorizationService,
    ILogger<RegisterTicketedEventHandler> logger)
    : ICommandHandler<RegisterTicketedEventCommand>
{
    public async ValueTask HandleAsync(RegisterTicketedEventCommand command, CancellationToken cancellationToken)
    {
        var teamSlug = await context.Teams
            .AsNoTracking()
            .Where(t => t.Id == command.TeamId)
            .Select(t => t.Slug)
            .FirstOrDefaultAsync(cancellationToken);

        if (teamSlug is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Team.NotFound(command.TeamId));
        }

        await authorizationService.AddTicketedEventAsync(teamSlug, command.TicketedEventSlug, cancellationToken);
        
        logger.LogInformation(
            "Registered ticketed event '{ticketedEvent}' for team '{team}' with the authorization service.",
            command.TicketedEventSlug,
            teamSlug);
    }
}