using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Amolenk.Admitto.Core.ArchTests;

[TestClass]
public class PersistenceConventionTests
{
    [TestMethod]
    public void EntityConfigurations_ShouldNotUseLambdaHasConversion()
    {
        var repoRoot = FindRepoRoot();
        var configFiles = Directory.GetFiles(
            Path.Combine(repoRoot, "src", "Admitto.Core"),
            "*EntityConfiguration.cs",
            SearchOption.AllDirectories);

        var violations = new List<string>();

        foreach (var file in configFiles)
        {
            var source = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(source, path: file, cancellationToken: TestContext.CancellationToken);
            var root = tree.GetRoot(TestContext.CancellationToken);

            var hasConversionCalls = root
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(invocation =>
                    invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "HasConversion" });

            foreach (var call in hasConversionCalls)
            {
                var hasLambdaArg = call.ArgumentList.Arguments.Any(arg =>
                    arg.Expression is SimpleLambdaExpressionSyntax or
                    ParenthesizedLambdaExpressionSyntax);

                if (!hasLambdaArg) continue;

                var lineSpan = tree.GetLineSpan(call.Span, TestContext.CancellationToken);
                var relativePath = Path.GetRelativePath(repoRoot, file);
                violations.Add($"{relativePath}:{lineSpan.StartLinePosition.Line + 1}");
            }
        }

        if (violations.Count > 0)
        {
            Assert.Fail(
                $"Found {violations.Count} HasConversion call(s) with lambda arguments in entity configuration files. " +
                $"Register Vogen value objects in DbContext.ConfigureConventions using HaveConversion<T.EfCoreValueConverter>() instead.\n\n" +
                string.Join("\n", violations));
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "src", "Admitto.Core")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            $"Could not find repository root from '{AppContext.BaseDirectory}'. " +
            "Expected to find src/Admitto.Core/ somewhere in the directory tree.");
    }

    public TestContext TestContext { get; set; }
}
