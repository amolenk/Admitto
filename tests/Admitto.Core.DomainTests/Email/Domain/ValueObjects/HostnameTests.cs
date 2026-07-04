using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Core.Email.Domain.Tests.ValueObjects;

[TestClass]
public sealed class HostnameTests
{
    [TestMethod]
    public void TryFrom_WithValidHost_TrimsAndSucceeds()
    {
        var result = Hostname.TryFrom("  smtp.example.com  ");

        result.IsSuccess.ShouldBeTrue();
        result.ValueObject.Value.ShouldBe("smtp.example.com");
    }

    [TestMethod]
    public void TryFrom_WithNull_Fails()
    {
        var result = Hostname.TryFrom(null!);

        result.IsSuccess.ShouldBeFalse();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void TryFrom_WithEmpty_Fails(string input)
    {
        var result = Hostname.TryFrom(input);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ErrorMessage.ShouldBe("Hostname is required.");
    }

    [TestMethod]
    public void TryFrom_OverMaxLength_Fails()
    {
        var result = Hostname.TryFrom(new string('a', Hostname.MaxLength + 1));

        result.IsSuccess.ShouldBeFalse();
        result.Error.ErrorMessage.ShouldBe($"Hostname must be at most {Hostname.MaxLength} character(s).");
    }
}
