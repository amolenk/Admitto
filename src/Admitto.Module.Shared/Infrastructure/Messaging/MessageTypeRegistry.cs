using System.Reflection;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Contracts;
using Humanizer;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Messaging;

/// <summary>
/// Maps the kebab-cased message type strings written to the queue
/// (see <c>OutboxWriter.GetMessageType</c>) back to their CLR <see cref="Type"/>
/// so the consumer can deserialize the payload.
/// </summary>
internal sealed class MessageTypeRegistry
{
    private readonly Dictionary<string, Entry> _byMessageType;

    public MessageTypeRegistry(IEnumerable<Assembly> assemblies)
    {
        _byMessageType = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in assemblies.Distinct())
        {
            foreach (var type in SafeGetTypes(assembly))
            {
                if (type.IsAbstract || type.IsInterface) continue;

                if (typeof(IIntegrationEvent).IsAssignableFrom(type))
                {
                    var key = BuildIntegrationKey(type);
                    _byMessageType[key] = new Entry(type, MessageKind.IntegrationEvent, ModuleNameFor(type));
                }
                else if (typeof(IModuleEvent).IsAssignableFrom(type))
                {
                    var key = BuildModuleEventKey(type);
                    _byMessageType[key] = new Entry(type, MessageKind.ModuleEvent, ModuleNameFor(type));
                }
            }
        }
    }

    public bool TryResolve(string messageType, out Entry entry) =>
        _byMessageType.TryGetValue(messageType, out entry!);

    public IReadOnlyDictionary<string, Entry> All => _byMessageType;

    public sealed record Entry(Type ClrType, MessageKind Kind, string ModuleName);

    public enum MessageKind
    {
        IntegrationEvent,
        ModuleEvent
    }

    private static string BuildIntegrationKey(Type type) =>
        $"integration.{ModuleNameFor(type).Kebaberize()}.{type.Name.Kebaberize()}";

    private static string BuildModuleEventKey(Type type) =>
        $"{ModuleNameFor(type).Kebaberize()}.{type.Name.Kebaberize()}";

    private static string ModuleNameFor(Type type)
    {
        // Expected: Amolenk.Admitto.Module.<ModuleName>.(Contracts.IntegrationEvents|Application.ModuleEvents)
        var ns = type.Namespace
                 ?? throw new InvalidOperationException($"Type {type.FullName} has no namespace.");
        var parts = ns.Split('.');
        if (parts.Length < 4 || parts[0] != "Amolenk" || parts[1] != "Admitto" || parts[2] != "Module")
        {
            throw new InvalidOperationException(
                $"Type {type.FullName} does not follow the expected module namespace convention.");
        }

        return parts[3];
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
