using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Module.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class TicketTypeTests
{
    private static TicketType CreateTicketType(int? maxCapacity = 10, int usedCapacity = 0)
    {
        var catalog = TicketCatalog.Create(TicketedEventId.New());
        catalog.AddTicketType(Slug.From("general"), DisplayName.From("General"), [], maxCapacity);
        var tt = catalog.GetTicketType("general")!;
        for (var i = 0; i < usedCapacity; i++)
            tt.ClaimUncapped();
        return tt;
    }

    [TestMethod]
    public void SC001_ReleaseCapacity_WhenUsedIsPositive_Decrements()
    {
        // Arrange
        var sut = CreateTicketType(maxCapacity: 10, usedCapacity: 5);

        // Act
        sut.ReleaseCapacity();

        // Assert
        sut.UsedCapacity.ShouldBe(4);
    }

    [TestMethod]
    public void SC002_ReleaseCapacity_WhenUsedIsOne_DecrementsToZero()
    {
        // Arrange
        var sut = CreateTicketType(maxCapacity: 10, usedCapacity: 1);

        // Act
        sut.ReleaseCapacity();

        // Assert
        sut.UsedCapacity.ShouldBe(0);
    }

    [TestMethod]
    public void SC003_ReleaseCapacity_WhenUsedIsZero_ClampsAtZero()
    {
        // Arrange
        var sut = CreateTicketType(maxCapacity: 10, usedCapacity: 0);

        // Act
        sut.ReleaseCapacity();

        // Assert
        sut.UsedCapacity.ShouldBe(0);
    }
}
