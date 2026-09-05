using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Core.Registrations.Domain.Tests.ValueObjects;

[TestClass]
public sealed class ReconfirmationEmailLimitTests
{
    // When a positive maximum reconfirmation email count is parsed
    // Then parsing succeeds and preserves the count
    [TestMethod]
    [DataRow(1)]
    [DataRow(5)]
    public void TryFrom_PositiveValue_Succeeds(int value)
    {
        var result = ReconfirmationEmailLimit.TryFrom(value);

        result.IsSuccess.ShouldBeTrue();
        result.ValueObject.Value.ShouldBe(value);
    }

    // When a non-positive maximum reconfirmation email count is parsed
    // Then parsing fails with the human-facing validation message
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void TryFrom_NonPositiveValue_Fails(int value)
    {
        var result = ReconfirmationEmailLimit.TryFrom(value);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ErrorMessage.ShouldBe("Maximum reconfirmation emails must be at least 1.");
    }
}
