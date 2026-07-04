using System.Reflection;
using System.Text.Json.Serialization;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.ArchTests;

[TestClass]
public class MessagingConventionTests
{
    private static readonly Assembly CoreAssembly = typeof(IIntegrationEvent).Assembly;

    [TestMethod]
    public void IntegrationEvents_WithMultiplePublicConstructors_HaveJsonConstructor()
    {
        var violations = CoreAssembly.GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsInterface: false } &&
                typeof(IIntegrationEvent).IsAssignableFrom(type))
            .Select(type => new
            {
                Type = type,
                Constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            })
            .Where(item => item.Constructors.Length > 1)
            .Where(item => item.Constructors.Count(HasJsonConstructorAttribute) != 1)
            .Select(item => item.Type.FullName)
            .OrderBy(name => name)
            .ToList();

        if (violations.Count > 0)
        {
            Assert.Fail(
                "Integration events with multiple public constructors must mark exactly one constructor with " +
                $"{nameof(JsonConstructorAttribute)} so queued messages can be deserialized.\n" +
                string.Join("\n", violations));
        }
    }

    private static bool HasJsonConstructorAttribute(ConstructorInfo constructor) =>
        constructor.GetCustomAttribute<JsonConstructorAttribute>() is not null;
}
