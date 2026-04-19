using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Domain.Tests.ValueObjects;

[TestClass]
public sealed class PortTests
{
    [TestMethod]
    [DataRow(1)]
    [DataRow(587)]
    [DataRow(65_535)]
    public void TryFrom_InRange_Succeeds(int port)
    {
        var result = Port.TryFrom(port);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(port);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(65_536)]
    public void TryFrom_OutOfRange_Fails(int port)
    {
        var result = Port.TryFrom(port);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("out_of_range");
    }
}
