using Amolenk.Admitto.Module.Shared.Contracts;

namespace Amolenk.Admitto.Module.Shared.Application.Messaging;

public interface IIntegrationEventHandler<in TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    ValueTask HandleAsync(TIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
