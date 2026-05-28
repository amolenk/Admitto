using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Amolenk.Admitto.Core.ArchTests;

[TestClass]
public class SecurityConventionTests
{
    /// <summary>
    /// Scans all handler files in UseCases/ directories and asserts that any lambda predicate
    /// passed to GetAsync or GetUntrackedAsync that references an event ID also includes a
    /// TeamId check, preventing cross-team data leakage.
    /// </summary>
    [TestMethod]
    public void HandlerPredicates_ReferencingEventId_MustAlsoReferenceTeamId()
    {
        var repoRoot = FindRepoRoot();
        var handlerFiles = Directory.GetFiles(
            Path.Combine(repoRoot, "src", "Admitto.Core"),
            "*Handler.cs",
            SearchOption.AllDirectories)
            .Where(f => f.Contains(Path.DirectorySeparatorChar + "UseCases" + Path.DirectorySeparatorChar));

        var violations = new List<string>();

        foreach (var file in handlerFiles)
        {
            var source = File.ReadAllText(file);

            // Self-service / attendee-facing handlers don't receive a teamId parameter from their
            // command — they derive TeamId from the event after the fact. Skip those files so the
            // test only flags admin handlers where teamId is actually in scope.
            if (!source.Contains("teamId"))
                continue;

            var tree = CSharpSyntaxTree.ParseText(source, path: file, cancellationToken: TestContext.CancellationToken);
            var root = tree.GetRoot(TestContext.CancellationToken);

            // Find all invocations of GetAsync or GetUntrackedAsync
            var getAsyncCalls = root
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(invocation =>
                    invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Name.Identifier.Text is "GetAsync" or "GetUntrackedAsync");

            foreach (var call in getAsyncCalls)
            {
                // Find lambda arguments in this call
                var lambdas = call.ArgumentList.Arguments
                    .Select(a => a.Expression)
                    .OfType<LambdaExpressionSyntax>();

                foreach (var lambda in lambdas)
                {
                    var lambdaText = lambda.ToString();

                    // If the lambda references EventId (in any form), it must also reference TeamId
                    bool referencesEventId = lambdaText.Contains("EventId", StringComparison.OrdinalIgnoreCase) ||
                                             lambdaText.Contains("eventId", StringComparison.Ordinal);

                    if (!referencesEventId) continue;

                    bool referencesTeamId = lambdaText.Contains("TeamId", StringComparison.OrdinalIgnoreCase) ||
                                            lambdaText.Contains("teamId", StringComparison.Ordinal);

                    if (referencesTeamId) continue;

                    // Allow this lambda if its enclosing method already has another GetAsync /
                    // GetUntrackedAsync call whose predicate contains BOTH eventId AND teamId.
                    // That prior call establishes the team-event scope, so a nested resource
                    // lookup (e.g., BadgeType by EventId) doesn't need to re-check TeamId.
                    var enclosingMethod = lambda.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                    if (enclosingMethod != null)
                    {
                        bool methodAlreadyScopesTeam = getAsyncCalls
                            .Where(c => c != call && enclosingMethod.Contains(c))
                            .SelectMany(c => c.ArgumentList.Arguments
                                .Select(a => a.Expression)
                                .OfType<LambdaExpressionSyntax>())
                            .Any(l =>
                            {
                                var t = l.ToString();
                                return (t.Contains("EventId", StringComparison.OrdinalIgnoreCase) ||
                                        t.Contains("eventId", StringComparison.Ordinal)) &&
                                       (t.Contains("TeamId", StringComparison.OrdinalIgnoreCase) ||
                                        t.Contains("teamId", StringComparison.Ordinal));
                            });

                        if (methodAlreadyScopesTeam) continue;
                    }

                    var lineSpan = tree.GetLineSpan(lambda.Span, TestContext.CancellationToken);
                    var relativePath = Path.GetRelativePath(repoRoot, file);
                    violations.Add($"{relativePath}:{lineSpan.StartLinePosition.Line + 1} — predicate references EventId but not TeamId: {lambdaText}");
                }
            }
        }

        if (violations.Count > 0)
        {
            Assert.Fail(
                $"Found {violations.Count} handler predicate(s) that reference EventId without also checking TeamId. " +
                $"This allows cross-team data access. Add a TeamId check to each predicate.\n\n" +
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
