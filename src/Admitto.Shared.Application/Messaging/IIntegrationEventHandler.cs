using Amolenk.Admitto.Shared.Contracts;

namespace Amolenk.Admitto.Shared.Application.Messaging;

public interface IIntegrationEventHandler<in TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    ValueTask HandleAsync(TIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
