using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypes.GetTicketTypes;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketTypes.GetTicketTypes;

[TestClass]
public sealed class GetTicketTypesTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a catalog with a mix of ticket types
    // When the ticket types are queried
    // Then all ticket types are returned with their details
    [TestMethod]
    public async ValueTask GetTicketTypes_ReturnsAllTypes()
    {
        // Arrange
        var fixture = GetTicketTypesFixture.WithMixedTicketTypes();
        await fixture.SetupAsync(Environment);

        var query = new GetTicketTypesQuery(fixture.EventId, fixture.TeamId);
        var sut = new GetTicketTypesHandler(Environment.RegistrationsDatabase.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);

        var active = result.Single(tt => tt.Id == fixture.GeneralAdmissionId.Value);
        active.Name.ShouldBe("General Admission");
        active.TimeSlots.ShouldContain("morning");
        active.MaxCapacity.ShouldBe(100);

        var vipPass = result.Single(tt => tt.Id == fixture.VipPassId.Value);
        vipPass.Name.ShouldBe("VIP Pass");
    }

    // Given an event with no ticket catalog
    // When the ticket types are queried
    // Then an empty list is returned
    [TestMethod]
    public async ValueTask GetTicketTypes_NoCatalog_ReturnsEmptyList()
    {
        // Arrange
        var fixture = GetTicketTypesFixture.NoCatalog();
        await fixture.SetupAsync(Environment);

        var query = new GetTicketTypesQuery(fixture.EventId, fixture.TeamId);
        var sut = new GetTicketTypesHandler(Environment.RegistrationsDatabase.Context);

        // Act
        var result = await sut.HandleAsync(query, testContext.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }
}
