using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;

internal static class TransactionalEmailComposerSubstituteExtensions
{
    public static void ReturnRenderedEmail(
        this ITransactionalEmailComposer composer,
        string emailType = BuiltInEmailTemplateNames.TicketConfirmation)
    {
        composer.ComposeAsync(
                Arg.Any<TransactionalEmailIntent>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new RenderedTransactionalEmail(
                emailType, "Subject", "Text", "<p>Html</p>")));
    }

    public static PrepareEmailDeliveryCommand ReceivedDelivery(
        this ICommandHandler<PrepareEmailDeliveryCommand> handler) =>
        (PrepareEmailDeliveryCommand)handler.ReceivedCalls().Single().GetArguments()[0]!;
}
