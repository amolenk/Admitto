using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Domain.Tests.ValueObjects;

[TestClass]
public sealed class HostnameTests
{
    [TestMethod]
    public void TryFrom_WithValidHost_TrimsAndSucceeds()
    {
        var result = Hostname.TryFrom("  smtp.example.com  ");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe("smtp.example.com");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void TryFrom_WithEmpty_FailsTextEmpty(string? input)
    {
        var result = Hostname.TryFrom(input);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldMatch(CommonErrors.TextEmpty);
    }

    [TestMethod]
    public void TryFrom_OverMaxLength_FailsTextTooLong()
    {
        var result = Hostname.TryFrom(new string('a', Hostname.MaxLength + 1));

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldMatch(CommonErrors.TextTooLong(Hostname.MaxLength));
    }
}
