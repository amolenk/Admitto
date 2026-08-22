using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Core.Registrations.Domain.Tests.ValueObjects;

[TestClass]
public sealed class AdditionalDetailsTests
{
    private static readonly AdditionalDetailSchema Schema = AdditionalDetailSchema.Create(new[]
    {
        AdditionalDetailField.Create("dietary", "Dietary", 200),
        AdditionalDetailField.Create("tshirt", "T-shirt", 5),
    });

    // Given a schema with defined fields
    // When null input is validated against it
    // Then it returns an empty result
    [TestMethod]
    public void Validate_Null_ReturnsEmpty()
    {
        AdditionalDetails.Validate(null, Schema).Count.ShouldBe(0);
    }

    // Given input values for all keys defined in the schema
    // When the input is validated
    // Then the returned values match what was provided for each key
    [TestMethod]
    public void Validate_AcceptedKeys_ReturnsValues()
    {
        var sut = AdditionalDetails.Validate(
            new Dictionary<string, string> { ["dietary"] = "vegan", ["tshirt"] = "M" }, Schema);

        sut.Count.ShouldBe(2);
        sut["dietary"].ShouldBe("vegan");
        sut["tshirt"].ShouldBe("M");
    }

    // Given input that only supplies a value for some of the schema's keys
    // When the input is validated
    // Then the omitted key is absent from the result
    [TestMethod]
    public void Validate_Partial_OmittedKeysAreNotProvided()
    {
        var sut = AdditionalDetails.Validate(
            new Dictionary<string, string> { ["dietary"] = "vegan" }, Schema);

        sut.Count.ShouldBe(1);
        sut.ContainsKey("tshirt").ShouldBeFalse();
    }

    // Given an input value that is an empty string for a schema key
    // When the input is validated
    // Then the empty string is preserved rather than dropped
    [TestMethod]
    public void Validate_EmptyString_Preserved()
    {
        var sut = AdditionalDetails.Validate(
            new Dictionary<string, string> { ["dietary"] = "" }, Schema);

        sut["dietary"].ShouldBe("");
    }

    // Given input containing a key that is not defined in the schema
    // When the input is validated
    // Then it throws a key-not-in-schema business rule violation
    [TestMethod]
    public void Validate_UnknownKey_Throws()
    {
        var act = () => AdditionalDetails.Validate(
            new Dictionary<string, string> { ["shoesize"] = "44" }, Schema);

        Should.Throw<BusinessRuleViolationException>(act)
            .Error.ShouldMatch(AdditionalDetails.Errors.KeyNotInSchema("shoesize"));
    }

    // Given input whose value exceeds the schema field's maximum length
    // When the input is validated
    // Then it throws a value-too-long business rule violation
    [TestMethod]
    public void Validate_ValueTooLong_Throws()
    {
        var act = () => AdditionalDetails.Validate(
            new Dictionary<string, string> { ["tshirt"] = "XXXXXX" }, Schema);

        Should.Throw<BusinessRuleViolationException>(act)
            .Error.ShouldMatch(AdditionalDetails.Errors.ValueTooLong("tshirt", 5));
    }
}
