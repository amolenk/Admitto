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
        _entries[key] = new MessageTypeRegistry.Entry(typeof(T), MessageTypeRegistry.MessageKind.Command, ModuleNameFor(typeof(T)));
        return this;
    }

    public MessageTypeRegistryBuilder AddIntegrationEvent<T>() where T : IIntegrationEvent
    {
        var key = BuildIntegrationKey(typeof(T));
        _entries[key] = new MessageTypeRegistry.Entry(typeof(T), MessageTypeRegistry.MessageKind.IntegrationEvent, ModuleNameFor(typeof(T)));
        return this;
    }

    internal MessageTypeRegistry Build() => new(_entries);

    private static string BuildIntegrationKey(Type type) =>
        $"integration.{ModuleNameFor(type).Kebaberize()}.{BuildIntegrationEventName(type).Kebaberize()}";

    private static string BuildCommandKey(Type type) =>
        $"command.{ModuleNameFor(type).Kebaberize()}.{BuildCommandName(type).Kebaberize()}";

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

    private static string ModuleNameFor(Type type)
    {
        var ns = type.Namespace ?? throw new InvalidOperationException($"Type {type.FullName} has no namespace.");
        var parts = ns.Split('.');
        if (parts.Length < 4 || parts[0] != "Amolenk" || parts[1] != "Admitto" || parts[2] != "Core")
            throw new InvalidOperationException($"Type {type.FullName} does not follow the expected module namespace convention.");
        return parts[3];
    }
}
