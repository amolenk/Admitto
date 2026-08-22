using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Core.Email.Domain.Tests.ValueObjects;

[TestClass]
public sealed class PortTests
{
    // When a port number within the valid range is parsed
    // Then it succeeds and the resulting value matches the input
    [TestMethod]
    [DataRow(1)]
    [DataRow(587)]
    [DataRow(65_535)]
    public void TryFrom_InRange_Succeeds(int port)
    {
        var result = Port.TryFrom(port);

        result.IsSuccess.ShouldBeTrue();
        result.ValueObject.Value.ShouldBe(port);
    }

    // When a port number outside the valid range is parsed
    // Then it fails with a message stating the allowed port range
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(65_536)]
    public void TryFrom_OutOfRange_Fails(int port)
    {
        var result = Port.TryFrom(port);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ErrorMessage.ShouldBe($"Port must be between {Port.MinValue} and {Port.MaxValue}.");
    }
}
