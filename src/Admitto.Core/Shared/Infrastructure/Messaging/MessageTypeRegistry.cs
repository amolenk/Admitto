using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Contracts;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;

/// <summary>
/// Maps the kebab-cased message type strings written to the queue back to their CLR
/// <see cref="Type"/> so the consumer can deserialize the payload.
/// </summary>
internal sealed class MessageTypeRegistry
{
    private readonly Dictionary<string, Entry> _byMessageType;

    internal MessageTypeRegistry(Dictionary<string, Entry> entries)
    {
        _byMessageType = entries;
    }

    public bool TryResolve(string messageType, out Entry entry) =>
        _byMessageType.TryGetValue(messageType, out entry!);

    public IReadOnlyDictionary<string, Entry> All => _byMessageType;

    public sealed record Entry(Type ClrType, MessageKind Kind, string ModuleName);

    public enum MessageKind
    {
        IntegrationEvent,
        Command
    }
}
