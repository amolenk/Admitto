using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;

public sealed class Outbox(IOutboxDbContext dbContext) : IOutbox
{
    public void Enqueue(ICommand command)
    {
        dbContext.OutboxMessages.Add(OutboxMessage.From(command));
    }

    public void Enqueue(IIntegrationEvent integrationEvent)
    {
        dbContext.OutboxMessages.Add(OutboxMessage.From(integrationEvent));
    }
}
