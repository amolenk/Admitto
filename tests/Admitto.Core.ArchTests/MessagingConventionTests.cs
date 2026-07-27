using System.Reflection;
using System.Text.Json.Serialization;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.ArchTests;

[TestClass]
public class MessagingConventionTests
{
    private static readonly Assembly CoreAssembly = typeof(IIntegrationEvent).Assembly;

    /// <summary>
    /// Message contracts — integration events, commands, and domain events — must declare exactly
    /// one public constructor carrying the complete field set.
    /// <para>
    /// For integration events and commands the reason is serialisation: both are written to the
    /// outbox as JSON (<c>OutboxMessage.From</c> accepts exactly these two) and rehydrated by the
    /// queue dispatcher, so their shape is a wire contract. A convenience constructor that defaults
    /// or drops fields compiles cleanly but emits a payload that cannot round-trip, and consumers
    /// silently observe defaulted values.
    /// </para>
    /// <para>
    /// Domain events are dispatched in-process and never serialised, so that argument does not
    /// apply — but the observed failure mode is worse. Their convenience constructors fabricated
    /// plausible-looking domain data (<c>EventName "Unknown event"</c>, <c>Slug "unknown-event"</c>,
    /// <c>FirstName "Unknown"</c>) which the integration-event publisher then copied onto the wire
    /// and into read-model projections. Placeholder values that look real are harder to spot than
    /// nulls, so the same single-constructor rule applies.
    /// </para>
    /// <para>
    /// Tests that want a terse construction path use a builder under
    /// <c>tests/Admitto.Testing/Builders/</c> instead of an overload on the contract.
    /// </para>
    /// </summary>
    [TestMethod]
    public void MessageContracts_DeclareExactlyOnePublicConstructor()
    {
        var violations = MessageContractTypes()
            .Select(item => new
            {
                item.Kind,
                item.Type,
                Constructors = PublicConstructors(item.Type)
            })
            .Where(item => item.Constructors.Length != 1)
            .Select(item =>
                $"[{item.Kind}] {item.Type.FullName} declares {item.Constructors.Length} public constructor(s):\n" +
                string.Join("\n", item.Constructors.Select(c => "    " + Describe(c))))
            .OrderBy(message => message)
            .ToList();

        if (violations.Count > 0)
        {
            Assert.Fail(
                "Message contracts must declare exactly one public constructor carrying the complete "
                + "field set. Add a builder under Admitto.Testing/Builders instead of a convenience "
                + "constructor.\n"
                + string.Join("\n", violations));
        }
    }

    /// <summary>
    /// Belt-and-braces for the single-constructor rule: if overloads are ever reintroduced on a
    /// serialised contract, exactly one must still be the designated deserialization target.
    /// </summary>
    [TestMethod]
    public void SerializedContracts_WithMultiplePublicConstructors_HaveJsonConstructor()
    {
        var violations = MessageContractTypes()
            .Where(item => item.Kind is not "IDomainEvent")
            .Select(item => new
            {
                item.Type,
                Constructors = PublicConstructors(item.Type)
            })
            .Where(item => item.Constructors.Length > 1)
            .Where(item => item.Constructors.Count(HasJsonConstructorAttribute) != 1)
            .Select(item => item.Type.FullName)
            .OrderBy(name => name)
            .ToList();

        if (violations.Count > 0)
        {
            Assert.Fail(
                "Serialized message contracts with multiple public constructors must mark exactly one "
                + $"constructor with {nameof(JsonConstructorAttribute)} so queued messages can be "
                + "deserialized.\n"
                + string.Join("\n", violations));
        }
    }

    private static IEnumerable<(string Kind, Type Type)> MessageContractTypes()
    {
        foreach (var type in ConcreteTypesAssignableTo(typeof(IIntegrationEvent)))
            yield return ("IIntegrationEvent", type);

        foreach (var type in ConcreteTypesAssignableTo(typeof(ICommand)))
            yield return ("ICommand", type);

        foreach (var type in ConcreteTypesAssignableTo(typeof(IDomainEvent)))
            yield return ("IDomainEvent", type);
    }

    private static IEnumerable<Type> ConcreteTypesAssignableTo(Type contract) =>
        CoreAssembly.GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsInterface: false } &&
                contract.IsAssignableFrom(type));

    /// <summary>
    /// Only genuinely declared constructors. The compiler-generated record copy constructor is
    /// protected, so it is already excluded by <see cref="BindingFlags.Public"/>.
    /// </summary>
    private static ConstructorInfo[] PublicConstructors(Type type) =>
        type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

    private static string Describe(ConstructorInfo constructor) =>
        "(" + string.Join(", ", constructor.GetParameters()
            .Select(p => $"{p.ParameterType.Name} {p.Name}")) + ")";

    private static bool HasJsonConstructorAttribute(ConstructorInfo constructor) =>
        constructor.GetCustomAttribute<JsonConstructorAttribute>() is not null;
}
