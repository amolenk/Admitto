using System.Reflection;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Contracts;
using Humanizer;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Messaging;

public sealed class MessageTypeRegistryBuilder
{
    private readonly Dictionary<string, MessageTypeRegistry.Entry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public MessageTypeRegistryBuilder AddCommand<T>() where T : ICommand
    {
        var key = BuildCommandKey(typeof(T));
        _entries[key] = new MessageTypeRegistry.Entry(typeof(T), MessageTypeRegistry.MessageKind.Command, MessageTypeRegistry.GetModuleKey(typeof(T)));
        return this;
    }

    public MessageTypeRegistryBuilder AddIntegrationEvent<T>() where T : IIntegrationEvent
    {
        var key = BuildIntegrationKey(typeof(T));
        _entries[key] = new MessageTypeRegistry.Entry(typeof(T), MessageTypeRegistry.MessageKind.IntegrationEvent, MessageTypeRegistry.GetModuleKey(typeof(T)));
        return this;
    }

    /// <summary>
    /// Scans for non-abstract concrete types in <paramref name="namespacePrefix"/>.*
    /// implementing <see cref="ICommand"/> or <see cref="IIntegrationEvent"/> and
    /// adds each to the registry — equivalent to calling
    /// <see cref="AddCommand{T}"/> / <see cref="AddIntegrationEvent{T}"/> per type.
    /// </summary>
    public MessageTypeRegistryBuilder AddFromAssembly(Assembly assembly, string namespacePrefix)
    {
        var commandType = typeof(ICommand);
        var integrationEventType = typeof(IIntegrationEvent);

        var candidates = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.Namespace is not null
                        && (t.Namespace == namespacePrefix || t.Namespace.StartsWith(namespacePrefix + ".", StringComparison.Ordinal)));

        foreach (var type in candidates)
        {
            if (integrationEventType.IsAssignableFrom(type))
            {
                var key = BuildIntegrationKey(type);
                _entries[key] = new MessageTypeRegistry.Entry(type, MessageTypeRegistry.MessageKind.IntegrationEvent, MessageTypeRegistry.GetModuleKey(type));
            }
            else if (commandType.IsAssignableFrom(type))
            {
                var key = BuildCommandKey(type);
                _entries[key] = new MessageTypeRegistry.Entry(type, MessageTypeRegistry.MessageKind.Command, MessageTypeRegistry.GetModuleKey(type));
            }
        }

        return this;
    }

    internal MessageTypeRegistry Build() => new(_entries);

    private static string BuildIntegrationKey(Type type) =>
        $"integration.{MessageTypeRegistry.GetModuleKey(type).Kebaberize()}.{BuildIntegrationEventName(type).Kebaberize()}";

    private static string BuildCommandKey(Type type) =>
        $"command.{MessageTypeRegistry.GetModuleKey(type).Kebaberize()}.{BuildCommandName(type).Kebaberize()}";

    private static string BuildIntegrationEventName(Type type)
    {
        const string suffix = "IntegrationEvent";
        return type.Name.EndsWith(suffix, StringComparison.Ordinal)
            ? type.Name[..^suffix.Length]
            : type.Name;
    }

    private static string BuildCommandName(Type type)
    {
        const string suffix = "Command";
        return type.Name.EndsWith(suffix, StringComparison.Ordinal)
            ? type.Name[..^suffix.Length]
            : type.Name;
    }
}
