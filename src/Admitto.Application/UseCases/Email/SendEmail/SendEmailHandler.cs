using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Composing;

namespace Amolenk.Admitto.Application.UseCases.Email.SendEmail;

public class SendEmailHandler(
    IEmailComposerRegistry emailComposerRegistry,
    IEmailDispatcher emailDispatcher)
    : ICommandHandler<SendEmailCommand>
{
    public async ValueTask HandleAsync(SendEmailCommand command, CancellationToken cancellationToken)
    {
        var emailComposer = emailComposerRegistry.GetEmailComposer(command.EmailType);

        var emailMessage = await emailComposer.ComposeMessageAsync(
            command.EmailType,
            command.TeamId,
            command.TicketedEventId,
            command.DataEntityId,
            command.AdditionalParameters,
            cancellationToken);

        await emailDispatcher.DispatchEmailAsync(
            emailMessage,
            command.TeamId,
            command.TicketedEventId,
            command.CommandId,
            cancellationToken: cancellationToken);
    }
}