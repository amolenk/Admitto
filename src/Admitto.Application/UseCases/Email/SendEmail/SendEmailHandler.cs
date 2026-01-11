using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Composing;
using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Application.Common.Persistence;

namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail;

[RequiresCapability(HostCapability.Email)]
public class SendEmailHandler(
    IEmailComposerRegistry emailComposerRegistry,
    IEmailDispatcher emailDispatcher,
    IApplicationContext context)
    : ICommandHandler<SendEmailCommand>
{
    public async ValueTask HandleAsync(SendEmailCommand command, CancellationToken cancellationToken)
    {
        var teamId = command.TeamId ?? await GetTeamIdAsync(command.TicketedEventId, cancellationToken);
        
        var emailComposer = emailComposerRegistry.GetEmailComposer(command.EmailType);

        var emailMessage = await emailComposer.ComposeMessageAsync(
            command.EmailType,
            teamId,
            command.TicketedEventId,
            command.DataEntityId,
            command.AdditionalParameters,
            cancellationToken);

        await emailDispatcher.DispatchEmailAsync(
            emailMessage,
            teamId,
            command.TicketedEventId,
            command.CommandId,
            cancellationToken: cancellationToken);
    }

    private async ValueTask<Guid> GetTeamIdAsync(Guid ticketedEventId, CancellationToken cancellationToken)
    {
        var teamId = await context.TicketedEvents
            .AsNoTracking()
            .Where(te => te.Id == ticketedEventId)
            .Select(te => te.TeamId)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (teamId == Guid.Empty)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }
        
        return teamId;
    }
}