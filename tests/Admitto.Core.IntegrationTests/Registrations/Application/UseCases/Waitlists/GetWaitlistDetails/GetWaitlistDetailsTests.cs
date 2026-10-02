using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.GetWaitlistDetails;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.GetWaitlistDetails;

[TestClass]
public sealed class GetWaitlistDetailsTests(TestContext testContext) : AspireIntegrationTestBase
{
    private GetWaitlistDetailsHandler CreateSut() => new(Environment.RegistrationsDatabase.Context);

    private static GetWaitlistDetailsQuery QueryFor(GetWaitlistDetailsFixture fixture) =>
        new(fixture.EventId.Value, fixture.TeamId.Value, fixture.TicketTypeId.Value);

    // Given an active waitlist entry whose email matches a real registration
    // When the waitlist details are retrieved
    // Then the row carries that registration's id and full attendee details, unmasked
    [TestMethod]
    public async ValueTask HandleAsync_ActiveEntryWithMatchingRegistration_ReturnsRegistrationIdAndFullDetails()
    {
        // Arrange
        var fixture = GetWaitlistDetailsFixture.WithOneActiveEntry();
        await fixture.SetupAsync(Environment);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(QueryFor(fixture), testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ActiveEntries.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            e => e.RegistrationId.ShouldBe(fixture.RegistrationId.Value),
            e => e.Email.ShouldBe(fixture.Email.Value),
            e => e.FirstName.ShouldBe("Alice"),
            e => e.LastName.ShouldBe("Doe"));
    }
}
