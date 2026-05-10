using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.MSTestV2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.ArchTests;

/// <summary>
/// Verifies that handler classes follow the naming conventions:
/// <list type="bullet">
///   <item>IDomainEventHandler&lt;T&gt; implementors → {T.Name}Handler</item>
///   <item>IIntegrationEventHandler&lt;T&gt; implementors → {T.Name}Handler or {T.Name}IntegrationEventHandler</item>
///   <item>ICommandHandler&lt;T&gt; implementors → command name with "Command" suffix replaced by "Handler"</item>
///   <item>IQueryHandler&lt;T,R&gt; implementors → query name with "Query" suffix replaced by "Handler"</item>
/// </list>
/// </summary>
[TestClass]
public class NamingRulesTests
{
    private static readonly Architecture Architecture = new ArchUnitNET.Loader.ArchLoader()
        .LoadAssemblies(typeof(DomainEvent).Assembly)
        .Build();

    private static readonly System.Reflection.Assembly CoreAssembly = typeof(DomainEvent).Assembly;

    [TestMethod]
    public void DomainEventHandlers_FollowNamingConvention()
    {
        var violations = CheckHandlerNaming(
            typeof(IDomainEventHandler<>),
            (eventTypeName, className) => className == $"{eventTypeName}Handler");

        if (violations.Count > 0)
            Assert.Fail(
                $"DomainEventHandler naming violations (expected {{EventType}}Handler):\n" +
                string.Join("\n", violations));
    }

    [TestMethod]
    public void IntegrationEventHandlers_FollowNamingConvention()
    {
        // Integration event types typically don't end in "IntegrationEvent", so the convention
        // allows {T.Name}Handler or {T.Name}IntegrationEventHandler for disambiguation.
        var violations = CheckHandlerNaming(
            typeof(IIntegrationEventHandler<>),
            (eventTypeName, className) =>
                className == $"{eventTypeName}Handler" ||
                className == $"{eventTypeName}IntegrationEventHandler");

        if (violations.Count > 0)
            Assert.Fail(
                $"IntegrationEventHandler naming violations (expected {{EventType}}Handler or {{EventType}}IntegrationEventHandler):\n" +
                string.Join("\n", violations));
    }

    [TestMethod]
    public void CommandHandlers_FollowNamingConvention()
    {
        // ICommandHandler<TCommand>: handler name = TCommand.Name with "Command" → "Handler".
        // Handles both ICommandHandler<T> and ICommandHandler<T,TResult>.
        var violations = CheckHandlerNaming(
            typeof(ICommandHandler<>),
            (commandTypeName, className) =>
            {
                var expected = commandTypeName.EndsWith("Command")
                    ? commandTypeName[..^"Command".Length] + "Handler"
                    : $"{commandTypeName}Handler";
                return className == expected;
            });

        var violations2 = CheckHandlerNaming2(
            typeof(ICommandHandler<,>),
            (commandTypeName, className) =>
            {
                var expected = commandTypeName.EndsWith("Command")
                    ? commandTypeName[..^"Command".Length] + "Handler"
                    : $"{commandTypeName}Handler";
                return className == expected;
            });

        violations.AddRange(violations2);

        if (violations.Count > 0)
            Assert.Fail(
                $"CommandHandler naming violations (expected command name with 'Command' replaced by 'Handler'):\n" +
                string.Join("\n", violations));
    }

    [TestMethod]
    public void QueryHandlers_FollowNamingConvention()
    {
        // IQueryHandler<TQuery,TResult>: handler name = TQuery.Name with "Query" → "Handler".
        var violations = CheckHandlerNaming2(
            typeof(IQueryHandler<,>),
            (queryTypeName, className) =>
            {
                var expected = queryTypeName.EndsWith("Query")
                    ? queryTypeName[..^"Query".Length] + "Handler"
                    : $"{queryTypeName}Handler";
                return className == expected;
            });

        if (violations.Count > 0)
            Assert.Fail(
                $"QueryHandler naming violations (expected query name with 'Query' replaced by 'Handler'):\n" +
                string.Join("\n", violations));
    }

    /// <summary>Checks naming for single-type-arg generic handler interfaces.</summary>
    private static List<string> CheckHandlerNaming(
        System.Type openGenericInterface,
        Func<string, string, bool> isValidName)
    {
        var violations = new List<string>();

        foreach (var type in CoreAssembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface) continue;

            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;
                if (iface.GetGenericTypeDefinition() != openGenericInterface) continue;

                var typeArgs = iface.GetGenericArguments();
                if (typeArgs.Length < 1) continue;

                var eventTypeName = typeArgs[0].Name;
                if (!isValidName(eventTypeName, type.Name))
                    violations.Add($"{type.FullName} (handles {eventTypeName})");

                break;
            }
        }

        return violations;
    }

    /// <summary>Checks naming for two-type-arg generic handler interfaces.</summary>
    private static List<string> CheckHandlerNaming2(
        System.Type openGenericInterface,
        Func<string, string, bool> isValidName)
    {
        var violations = new List<string>();

        foreach (var type in CoreAssembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface) continue;

            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;
                if (iface.GetGenericTypeDefinition() != openGenericInterface) continue;

                var typeArgs = iface.GetGenericArguments();
                if (typeArgs.Length < 1) continue;

                var firstArgName = typeArgs[0].Name;
                if (!isValidName(firstArgName, type.Name))
                    violations.Add($"{type.FullName} (handles {firstArgName})");

                break;
            }
        }

        return violations;
    }
}
