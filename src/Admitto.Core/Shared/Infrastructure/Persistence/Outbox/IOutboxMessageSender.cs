namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;

public interface IOutboxMessageSender
{
    ValueTask SendAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}