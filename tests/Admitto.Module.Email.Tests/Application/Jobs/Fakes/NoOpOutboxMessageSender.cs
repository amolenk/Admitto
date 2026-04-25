using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Outbox;

namespace Amolenk.Admitto.Module.Email.Tests.Application.Jobs.Fakes;

internal sealed class NoOpOutboxMessageSender : IOutboxMessageSender
{
    public ValueTask SendAsync(OutboxMessage message, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;
}
