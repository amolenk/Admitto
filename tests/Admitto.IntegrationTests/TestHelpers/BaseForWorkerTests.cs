using Microsoft.Extensions.Hosting;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers;

[TestClass, DoNotParallelize]
public abstract class BaseForWorkerTests : BaseForFullStackTests
{
    // Convenience property for accessing the worker host
    protected static readonly IHost WorkerHost = AssemblyTestFixture.WorkerHost;
}