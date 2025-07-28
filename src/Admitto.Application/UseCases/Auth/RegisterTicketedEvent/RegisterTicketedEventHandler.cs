namespace Amolenk.Admitto.Application.UseCases.Auth.RegisterTicketedEvent;

public class RegisterTicketedEventHandler(IApplicationContext context, IAuthorizationService authorizationService) 
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
            // TODO Log
            return;
        }
        
        await authorizationService.AddTicketedEventAsync(teamSlug, command.EventSlug, cancellationToken); 
    }
}
