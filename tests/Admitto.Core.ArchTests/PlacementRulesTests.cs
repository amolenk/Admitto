using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.ArchTests;

/// <summary>
/// Verifies that classes reside in the correct namespaces:
/// <list type="bullet">
///   <item>*DomainEventHandler, *IntegrationEventHandler, *ModuleEventHandler → namespace must contain "EventHandlers"</item>
///   <item>*HttpEndpoint → namespace must contain "AdminApi", "PublicApi", or "InternalApi"</item>
///   <item>AbstractValidator&lt;T&gt; subclasses → namespace must contain "AdminApi", "PublicApi", or "InternalApi"</item>
///   <item>*Command and *Query classes → namespace must match *.Application.UseCases.*</item>
/// </list>
/// </summary>
[TestClass]
public class PlacementRulesTests
{
    private static readonly Architecture Architecture = new ArchUnitNET.Loader.ArchLoader()
        .LoadAssemblies(typeof(DomainEvent).Assembly)
        .Build();

    [TestMethod]
    public void EventHandlers_MustResideInEventHandlersNamespace()
    {
        AssertRule(Classes().That()
            .HaveNameEndingWith("DomainEventHandler")
            .Or().HaveNameEndingWith("IntegrationEventHandler")
            .Or().HaveNameEndingWith("ModuleEventHandler")
            .Should().ResideInNamespaceMatching(".*\\.EventHandlers($|\\..*)"));
    }

    [TestMethod]
    public void HttpEndpoints_MustResideInApiNamespace()
    {
        AssertRule(Classes().That()
            .HaveNameEndingWith("HttpEndpoint")
            .Should()
            .ResideInNamespaceMatching(".*\\.(AdminApi|PublicApi|InternalApi)($|\\..*)"));
    }

    [TestMethod]
    public void Validators_MustResideInApiNamespace()
    {
        AssertRule(Classes().That()
            .AreAssignableTo(typeof(FluentValidation.AbstractValidator<>))
            .And().DoNotHaveNameEndingWith("AbstractValidator")
            .Should()
            .ResideInNamespaceMatching(".*\\.(AdminApi|PublicApi|InternalApi)($|\\..*)"));
    }

    [TestMethod]
    public void Validators_MustNotBeNestedClasses()
    {
        var validatorBase = typeof(FluentValidation.AbstractValidator<>);

        var violations = System.Reflection.Assembly.GetAssembly(typeof(DomainEvent))!
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsNested: true }
                        && IsAssignableToOpenGeneric(t, validatorBase))
            .Select(t => t.FullName!)
            .ToList();

        if (violations.Count > 0)
            Assert.Fail(
                "Validators must be top-level classes, not nested inside request types:\n" +
                string.Join("\n", violations));
    }

    private static bool IsAssignableToOpenGeneric(System.Type type, System.Type openGeneric)
    {
        for (var t = type; t is not null; t = t.BaseType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == openGeneric)
                return true;
        }
        return false;
    }

    [TestMethod]
    public void Commands_MustResideInUseCasesNamespace()
    {
        AssertRule(Classes().That()
            .HaveNameEndingWith("Command")
            .And().ResideInNamespace("Amolenk.Admitto.Core")
            .Should()
            .ResideInNamespaceMatching(".*\\.Application\\.UseCases\\..*")
            .WithoutRequiringPositiveResults());
    }

    [TestMethod]
    public void Queries_MustResideInUseCasesNamespace()
    {
        AssertRule(Classes().That()
            .HaveNameEndingWith("Query")
            .And().ResideInNamespace("Amolenk.Admitto.Core")
            .Should()
            .ResideInNamespaceMatching(".*\\.Application\\.UseCases\\..*")
            .WithoutRequiringPositiveResults());
    }

    private static void AssertRule(IArchRule rule)
    {
        if (rule.HasNoViolations(Architecture)) return;

        var failures = rule.Evaluate(Architecture)
            .Where(r => !r.Passed)
            .Select(r => r.Description)
            .ToList();

        Assert.Fail("Architecture violations:\n" + string.Join("\n", failures));
    }
}
