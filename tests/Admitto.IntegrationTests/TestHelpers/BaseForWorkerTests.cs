using Amolenk.Admitto.Application.Common.Abstractions;
using Microsoft.Extensions.Hosting;

namespace Amolenk.Admitto.IntegrationTests.TestHelpers;

[TestClass]
public abstract class BaseForWorkerTests : BaseForFullStackTests
{
    protected IUnitOfWork UnitOfWork = null!;
    
    private IServiceScope _workerHostServiceScope = null!;

    protected IServiceProvider WorkerHostServiceProvider => _workerHostServiceScope.ServiceProvider;
    
    [TestInitialize]
    public override Task TestInitialize()
    {
        _workerHostServiceScope = AssemblyTestFixture.WorkerHost.Services.CreateScope();
        UnitOfWork = _workerHostServiceScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        return base.TestInitialize();
    }
    
    [TestCleanup]
    public void TestCleanup()
    {
        _workerHostServiceScope.Dispose();
    }
}