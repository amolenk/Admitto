using Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Core.Module.Email.Domain.Tests.ValueObjects;

[TestClass]
public sealed class SmtpUsernameTests
{
    [TestMethod]
    public void TryFrom_WithValidUsername_TrimsAndSucceeds()
    {
        var result = SmtpUsername.TryFrom("  alice  ");

        result.IsSuccess.ShouldBeTrue();
        result.ValueObject.Value.ShouldBe("alice");
    }

    [TestMethod]
    public void TryFrom_WithNull_Fails()
    {
        var result = SmtpUsername.TryFrom(null);

        result.IsSuccess.ShouldBeFalse();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void TryFrom_WithEmpty_Fails(string input)
    {
        var result = SmtpUsername.TryFrom(input);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ErrorMessage.ShouldBe("SMTP username is required.");
    }

    [TestMethod]
    public void TryFrom_OverMaxLength_Fails()
    {
        var result = SmtpUsername.TryFrom(new string('u', SmtpUsername.MaxLength + 1));

        result.IsSuccess.ShouldBeFalse();
        result.Error.ErrorMessage.ShouldBe($"SMTP username must be at most {SmtpUsername.MaxLength} character(s).");
    }
}
