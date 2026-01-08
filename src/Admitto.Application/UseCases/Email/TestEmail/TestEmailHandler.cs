using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Email.Composing;
using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email.TestEmail;

public class TestEmailHandler(
    IEmailComposerRegistry emailComposerRegistry,
    IEmailDispatcher emailDispatcher,
    IApplicationContext context)
    : ICommandHandler<TestEmailCommand>, IWorkerHandler
{
    public async ValueTask HandleAsync(TestEmailCommand command, CancellationToken cancellationToken)
    {
        var emailComposer = emailComposerRegistry.GetEmailComposer(command.EmailType);

        var emailMessage = await emailComposer.ComposeTestMessageAsync(
            command.EmailType,
            command.TeamId,
            command.TicketedEventId,
            command.Recipient,
            command.AdditionalDetails
                .Select(ad => new AdditionalDetail(ad.Name, ad.Value))
                .ToList(),
            command.Tickets
                .Select(t => new TicketSelection(t.TicketTypeSlug, t.Quantity))
                .ToList(),
            cancellationToken);
        
        await emailDispatcher.DispatchEmailAsync(
            emailMessage,
            command.TeamId,
            command.TicketedEventId,
            EmailDispatcher.TestMessageIdempotencyKey,
            cancellationToken: cancellationToken);
    }
}