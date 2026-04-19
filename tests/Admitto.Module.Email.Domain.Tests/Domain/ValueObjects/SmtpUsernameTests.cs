using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Domain.Tests.ValueObjects;

[TestClass]
public sealed class SmtpUsernameTests
{
    [TestMethod]
    public void TryFrom_WithValidUsername_TrimsAndSucceeds()
    {
        var result = SmtpUsername.TryFrom("  alice  ");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe("alice");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void TryFrom_WithEmpty_FailsTextEmpty(string? input)
    {
        var result = SmtpUsername.TryFrom(input);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldMatch(CommonErrors.TextEmpty);
    }

    [TestMethod]
    public void TryFrom_OverMaxLength_FailsTextTooLong()
    {
        var result = SmtpUsername.TryFrom(new string('u', SmtpUsername.MaxLength + 1));

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldMatch(CommonErrors.TextTooLong(SmtpUsername.MaxLength));
    }
}
