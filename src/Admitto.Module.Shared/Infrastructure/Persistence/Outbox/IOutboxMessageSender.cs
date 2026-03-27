namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Outbox;

public interface IOutboxMessageSender
{
    ValueTask SendAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}