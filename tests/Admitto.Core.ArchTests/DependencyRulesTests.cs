using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.ArchTests;

[TestClass]
public class DependencyRulesTests
{
    private static readonly Architecture Architecture = new ArchUnitNET.Loader.ArchLoader()
        .LoadAssemblies(typeof(DomainEvent).Assembly)
        .Build();

    // SharedKernel (Amolenk.Admitto.Core.Shared.Kernel.*) must not depend on
    // anything else inside Amolenk.Admitto.Core.
    [TestMethod]
    public void SharedKernel_HasNoDependenciesOnOtherAdmittoCore()
    {
        var nonSharedKernelCoreTypes = Types().That()
            .ResideInNamespace("Amolenk.Admitto.Core")
            .And().DoNotResideInNamespace("Amolenk.Admitto.Core.Shared.Kernel");

        AssertRule(Classes().That()
            .ResideInNamespace("Amolenk.Admitto.Core.Shared.Kernel")
            .Should().NotDependOnAny(nonSharedKernelCoreTypes)
            .WithoutRequiringPositiveResults());
    }

    // Domain layers must not depend on Application or Infrastructure layers.
    [TestMethod]
    public void DomainLayer_HasNoDependenciesOnApplicationOrInfrastructure()
    {
        var appOrInfraTypes = Types().That()
            .HaveFullNameContaining(".Application.")
            .Or().HaveFullNameContaining(".Infrastructure.");

        AssertRule(Classes().That()
            .HaveFullNameContaining(".Domain.")
            .Should().NotDependOnAny(appOrInfraTypes));
    }

    // Application layers must not depend on other modules' Domain, Application, or
    // Infrastructure types — only Contracts cross-module references are permitted.
    [TestMethod]
    public void ApplicationLayer_DoesNotDependOnOtherModuleNonContractTypes()
    {
        var violations = new List<string>();
        var knownModules = new[] { "Organization", "Registrations", "Email", "Badges" };

        foreach (var sourceModule in knownModules)
        {
            var appPrefix = $"Amolenk.Admitto.Core.{sourceModule}.Application.";

            var appClasses = Architecture.Classes
                .Where(c => c.Namespace?.FullName?.StartsWith(appPrefix) == true)
                .ToList();

            foreach (var @class in appClasses)
            {
                foreach (var dep in @class.Dependencies)
                {
                    var targetNs = dep.Target.Namespace?.FullName;
                    if (targetNs is null) continue;

                    foreach (var targetModule in knownModules)
                    {
                        if (targetModule == sourceModule) continue;

                        var moduleNsPrefix = $"Amolenk.Admitto.Core.{targetModule}.";
                        if (!targetNs.StartsWith(moduleNsPrefix)) continue;

                        // Cross-module reference: only Contracts is allowed.
                        if (!targetNs.StartsWith($"{moduleNsPrefix}Contracts"))
                        {
                            violations.Add(
                                $"{@class.FullName} -> {dep.Target.FullName}");
                        }
                    }
                }
            }
        }

        if (violations.Count > 0)
            Assert.Fail(
                $"Application layer has forbidden cross-module dependencies:\n" +
                string.Join("\n", violations));
    }

    // Infrastructure layers must not depend on other modules' Domain, Application, or
    // Infrastructure types — only Contracts cross-module references are permitted.
    [TestMethod]
    public void InfrastructureLayer_DoesNotDependOnOtherModuleNonContractTypes()
    {
        var violations = new List<string>();
        var knownModules = new[] { "Organization", "Registrations", "Email", "Badges" };

        foreach (var sourceModule in knownModules)
        {
            var infraPrefix = $"Amolenk.Admitto.Core.{sourceModule}.Infrastructure.";

            var infraClasses = Architecture.Classes
                .Where(c => c.Namespace?.FullName?.StartsWith(infraPrefix) == true)
                .ToList();

            foreach (var @class in infraClasses)
            {
                foreach (var dep in @class.Dependencies)
                {
                    var targetNs = dep.Target.Namespace?.FullName;
                    if (targetNs is null) continue;

                    foreach (var targetModule in knownModules)
                    {
                        if (targetModule == sourceModule) continue;

                        var moduleNsPrefix = $"Amolenk.Admitto.Core.{targetModule}.";
                        if (!targetNs.StartsWith(moduleNsPrefix)) continue;

                        if (!targetNs.StartsWith($"{moduleNsPrefix}Contracts"))
                        {
                            violations.Add(
                                $"{@class.FullName} -> {dep.Target.FullName}");
                        }
                    }
                }
            }
        }

        if (violations.Count > 0)
            Assert.Fail(
                $"Infrastructure layer has forbidden cross-module dependencies:\n" +
                string.Join("\n", violations));
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
