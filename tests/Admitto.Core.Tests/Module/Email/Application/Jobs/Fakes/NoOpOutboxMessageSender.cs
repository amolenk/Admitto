using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;

namespace Amolenk.Admitto.Core.Email.Tests.Application.Jobs.Fakes;

internal sealed class NoOpOutboxMessageSender : IOutboxMessageSender
{
    public ValueTask SendAsync(OutboxMessage message, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;
}
