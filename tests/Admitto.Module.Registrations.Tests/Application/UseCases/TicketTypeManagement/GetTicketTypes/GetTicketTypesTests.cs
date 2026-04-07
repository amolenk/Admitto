using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.GetTicketTypes;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketTypeManagement.GetTicketTypes;

[TestClass]
public sealed class GetTicketTypesTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-001: List ticket types with mixed active/cancelled — returns all with correct flags
    [TestMethod]
    public async ValueTask SC001_GetTicketTypes_MixedActiveAndCancelled_ReturnsAllWithCorrectFlags()
    {
        // Arrange
        var fixture = GetTicketTypesFixture.WithMixedTicketTypes();
        await fixture.SetupAsync(Environment);

        var query = new GetTicketTypesQuery(fixture.EventId);
        var sut = new GetTicketTypesHandler(Environment.Database.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);

        var active = result.Single(tt => tt.Slug == "general-admission");
        active.Name.ShouldBe("General Admission");
        active.TimeSlots.ShouldContain("morning");
        active.MaxCapacity.ShouldBe(100);
        active.IsCancelled.ShouldBeFalse();

        var cancelled = result.Single(tt => tt.Slug == "vip-pass");
        cancelled.Name.ShouldBe("VIP Pass");
        cancelled.IsCancelled.ShouldBeTrue();
    }

    // SC-002: Empty list when no catalog exists — returns empty list
    [TestMethod]
    public async ValueTask SC002_GetTicketTypes_NoCatalog_ReturnsEmptyList()
    {
        // Arrange
        var fixture = GetTicketTypesFixture.NoCatalog();
        await fixture.SetupAsync(Environment);

        var query = new GetTicketTypesQuery(fixture.EventId);
        var sut = new GetTicketTypesHandler(Environment.Database.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }
}
