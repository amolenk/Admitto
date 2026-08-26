using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Core.Email.Domain.Tests.ValueObjects;

[TestClass]
public sealed class HostnameTests
{
    // Given a valid hostname with surrounding whitespace
    // When it is parsed
    // Then it succeeds and the value is trimmed
    [TestMethod]
    public void TryFrom_WithValidHost_TrimsAndSucceeds()
    {
        var result = Hostname.TryFrom("  smtp.example.com  ");

        result.IsSuccess.ShouldBeTrue();
        result.ValueObject.Value.ShouldBe("smtp.example.com");
    }

    // When a hostname is parsed from a null value
    // Then it fails
    [TestMethod]
    public void TryFrom_WithNull_Fails()
    {
        var result = Hostname.TryFrom(null!);

        result.IsSuccess.ShouldBeFalse();
    }

    // When a hostname is parsed from an empty or whitespace-only value
    // Then it fails with a "Hostname is required" error
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void TryFrom_WithEmpty_Fails(string input)
    {
        var result = Hostname.TryFrom(input);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ErrorMessage.ShouldBe("Hostname is required.");
    }

    // When a hostname longer than the maximum allowed length is parsed
    // Then it fails with a maximum-length error
    [TestMethod]
    public void TryFrom_OverMaxLength_Fails()
    {
        var result = Hostname.TryFrom(new string('a', Hostname.MaxLength + 1));

        result.IsSuccess.ShouldBeFalse();
        result.Error.ErrorMessage.ShouldBe($"Hostname must be at most {Hostname.MaxLength} character(s).");
    }
}
