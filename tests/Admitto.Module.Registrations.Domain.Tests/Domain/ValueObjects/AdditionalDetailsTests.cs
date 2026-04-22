using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Shouldly;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Module.Registrations.Domain.Tests.ValueObjects;

[TestClass]
public sealed class AdditionalDetailsTests
{
    private static readonly AdditionalDetailSchema Schema = AdditionalDetailSchema.Create(new[]
    {
        AdditionalDetailField.Create("dietary", "Dietary", 200),
        AdditionalDetailField.Create("tshirt", "T-shirt", 5),
    });

    [TestMethod]
    public void SC001_Validate_Null_ReturnsEmpty()
    {
        AdditionalDetails.Validate(null, Schema).Count.ShouldBe(0);
    }

    [TestMethod]
    public void SC002_Validate_AcceptedKeys_ReturnsValues()
    {
        var sut = AdditionalDetails.Validate(
            new Dictionary<string, string> { ["dietary"] = "vegan", ["tshirt"] = "M" }, Schema);

        sut.Count.ShouldBe(2);
        sut["dietary"].ShouldBe("vegan");
        sut["tshirt"].ShouldBe("M");
    }

    [TestMethod]
    public void SC003_Validate_Partial_OmittedKeysAreNotProvided()
    {
        var sut = AdditionalDetails.Validate(
            new Dictionary<string, string> { ["dietary"] = "vegan" }, Schema);

        sut.Count.ShouldBe(1);
        sut.ContainsKey("tshirt").ShouldBeFalse();
    }

    [TestMethod]
    public void SC004_Validate_EmptyString_Preserved()
    {
        var sut = AdditionalDetails.Validate(
            new Dictionary<string, string> { ["dietary"] = "" }, Schema);

        sut["dietary"].ShouldBe("");
    }

    [TestMethod]
    public void SC010_Validate_UnknownKey_Throws()
    {
        var act = () => AdditionalDetails.Validate(
            new Dictionary<string, string> { ["shoesize"] = "44" }, Schema);

        Should.Throw<BusinessRuleViolationException>(act)
            .Error.Code.ShouldBe("additional_details.key_not_in_schema");
    }

    [TestMethod]
    public void SC011_Validate_ValueTooLong_Throws()
    {
        var act = () => AdditionalDetails.Validate(
            new Dictionary<string, string> { ["tshirt"] = "XXXXXX" }, Schema);

        Should.Throw<BusinessRuleViolationException>(act)
            .Error.Code.ShouldBe("additional_details.value_too_long");
    }
}
