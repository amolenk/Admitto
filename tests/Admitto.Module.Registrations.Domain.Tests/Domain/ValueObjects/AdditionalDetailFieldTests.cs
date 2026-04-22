using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Shouldly;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Module.Registrations.Domain.Tests.ValueObjects;

[TestClass]
public sealed class AdditionalDetailFieldTests
{
    [TestMethod]
    public void SC001_Create_Valid_ReturnsField()
    {
        var sut = AdditionalDetailField.Create("dietary", "Dietary requirements", 200);

        sut.Key.ShouldBe("dietary");
        sut.Name.ShouldBe("Dietary requirements");
        sut.MaxLength.ShouldBe(200);
    }

    [TestMethod]
    public void SC002_Create_TrimsName()
    {
        var sut = AdditionalDetailField.Create("k", "  Padded  ", 10);

        sut.Name.ShouldBe("Padded");
    }

    [DataRow("Dietary")]
    [DataRow("dietary needs")]
    [DataRow("-dietary")]
    [DataRow("")]
    [DataRow("dietary_needs")]
    [TestMethod]
    public void SC010_Create_InvalidKey_Throws(string key)
    {
        var act = () => AdditionalDetailField.Create(key, "Name", 10);

        Should.Throw<BusinessRuleViolationException>(act)
            .Error.Code.ShouldBe("additional_detail_field.invalid_key");
    }

    [TestMethod]
    public void SC011_Create_KeyTooLong_Throws()
    {
        var act = () => AdditionalDetailField.Create(new string('a', 51), "Name", 10);

        Should.Throw<BusinessRuleViolationException>(act)
            .Error.Code.ShouldBe("additional_detail_field.invalid_key");
    }

    [TestMethod]
    public void SC020_Create_EmptyName_Throws()
    {
        var act = () => AdditionalDetailField.Create("k", "   ", 10);

        Should.Throw<BusinessRuleViolationException>(act)
            .Error.Code.ShouldBe("additional_detail_field.name_empty");
    }

    [TestMethod]
    public void SC021_Create_NameTooLong_Throws()
    {
        var act = () => AdditionalDetailField.Create("k", new string('a', 101), 10);

        Should.Throw<BusinessRuleViolationException>(act)
            .Error.Code.ShouldBe("additional_detail_field.name_too_long");
    }

    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(4001)]
    [TestMethod]
    public void SC030_Create_MaxLengthOutOfRange_Throws(int maxLength)
    {
        var act = () => AdditionalDetailField.Create("k", "Name", maxLength);

        Should.Throw<BusinessRuleViolationException>(act)
            .Error.Code.ShouldBe("additional_detail_field.max_length_out_of_range");
    }
}
