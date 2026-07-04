namespace Amolenk.Admitto.Core.Shared.Application.Messaging;

public interface IInbox
{
    ValueTask<bool> TryMarkAsProcessedByAsync<THandler>(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken);
}
