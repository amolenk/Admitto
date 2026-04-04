using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Module.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class EventCapacityTests
{
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();

    [TestMethod]
    public void SC001_TicketCapacity_ClaimWithEnforcement_NullMaxCapacity_Throws()
    {
        // Arrange
        var sut = TicketCapacity.Create("general-admission", maxCapacity: null);

        // Act
        var result = ErrorResult.Capture(() => sut.ClaimWithEnforcement());

        // Assert
        result.Error.ShouldMatch(TicketCapacity.Errors.TicketTypeNotAvailable("general-admission"));
    }

    [TestMethod]
    public void SC002_TicketCapacity_ClaimWithEnforcement_AtCapacity_Throws()
    {
        // Arrange
        var sut = TicketCapacity.Create("general-admission", maxCapacity: 1);
        sut.ClaimWithEnforcement(); // fills the single slot

        // Act
        var result = ErrorResult.Capture(() => sut.ClaimWithEnforcement());

        // Assert
        result.Error.ShouldMatch(TicketCapacity.Errors.TicketTypeAtCapacity("general-admission"));
    }

    [TestMethod]
    public void SC003_TicketCapacity_ClaimWithEnforcement_AvailableCapacity_Increments()
    {
        // Arrange
        var sut = TicketCapacity.Create("general-admission", maxCapacity: 10);

        // Act
        sut.ClaimWithEnforcement();

        // Assert
        sut.UsedCapacity.ShouldBe(1);
    }

    [TestMethod]
    public void SC004_TicketCapacity_ClaimUncapped_NullMaxCapacity_Increments()
    {
        // Arrange
        var sut = TicketCapacity.Create("vip-pass", maxCapacity: null);

        // Act
        sut.ClaimUncapped();

        // Assert
        sut.UsedCapacity.ShouldBe(1);
    }

    [TestMethod]
    public void SC005_TicketCapacity_ClaimUncapped_AtCapacity_StillIncrements()
    {
        // Arrange
        var sut = TicketCapacity.Create("vip-pass", maxCapacity: 1);
        sut.ClaimUncapped(); // now at capacity

        // Act
        sut.ClaimUncapped();

        // Assert
        sut.UsedCapacity.ShouldBe(2);
    }

    [TestMethod]
    public void SC006_EventCapacity_Claim_MultipleSlug_EnforcePath_AllIncrement()
    {
        // Arrange
        var sut = EventCapacity.Create(DefaultEventId);
        sut.SetTicketCapacity("slug-a", 10);
        sut.SetTicketCapacity("slug-b", 10);

        // Act
        sut.Claim(["slug-a", "slug-b"], enforce: true);

        // Assert
        sut.TicketCapacities.Single(tc => tc.Id == "slug-a").UsedCapacity.ShouldBe(1);
        sut.TicketCapacities.Single(tc => tc.Id == "slug-b").UsedCapacity.ShouldBe(1);
    }

    [TestMethod]
    public void SC007_EventCapacity_Claim_UnknownSlug_Throws()
    {
        // Arrange
        var sut = EventCapacity.Create(DefaultEventId);
        sut.SetTicketCapacity("known-slug", 10);

        // Act
        var result = ErrorResult.Capture(() => sut.Claim(["unknown-slug"], enforce: true));

        // Assert
        result.Error.Code.ShouldBe("ticket_capacity_not_found");
    }

    [TestMethod]
    public void SC008_EventCapacity_SetTicketCapacity_NewSlug_Adds()
    {
        // Arrange
        var sut = EventCapacity.Create(DefaultEventId);

        // Act
        sut.SetTicketCapacity("new-slug", 50);

        // Assert
        sut.TicketCapacities.Count.ShouldBe(1);
        sut.TicketCapacities.Single().Id.ShouldBe("new-slug");
        sut.TicketCapacities.Single().MaxCapacity.ShouldBe(50);
    }

    [TestMethod]
    public void SC009_EventCapacity_SetTicketCapacity_ExistingSlug_Updates()
    {
        // Arrange
        var sut = EventCapacity.Create(DefaultEventId);
        sut.SetTicketCapacity("existing-slug", 10);

        // Act
        sut.SetTicketCapacity("existing-slug", 99);

        // Assert
        sut.TicketCapacities.Count.ShouldBe(1);
        sut.TicketCapacities.Single().MaxCapacity.ShouldBe(99);
    }
}
