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
        result.WaitlistEnabled.ShouldBeTrue();
        result.ActiveEntries.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            e => e.RegistrationId.ShouldBe(fixture.RegistrationId.Value),
            e => e.Email.ShouldBe(fixture.Email.Value),
            e => e.FirstName.ShouldBe("Alice"),
            e => e.LastName.ShouldBe("Doe"));
    }

    // Given an entry with an outstanding coupon offer whose email matches a real registration
    // When the waitlist details are retrieved
    // Then the pending notification row carries that registration's id and full attendee details, unmasked
    [TestMethod]
    public async ValueTask HandleAsync_PendingNotificationWithMatchingRegistration_ReturnsRegistrationIdAndFullDetails()
    {
        // Arrange
        var fixture = GetWaitlistDetailsFixture.WithOnePendingNotification();
        await fixture.SetupAsync(Environment);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(QueryFor(fixture), testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.ActiveEntries.ShouldBeEmpty();
        result.PendingNotifications.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            n => n.RegistrationId.ShouldBe(fixture.RegistrationId.Value),
            n => n.Email.ShouldBe(fixture.Email.Value),
            n => n.FirstName.ShouldBe("Alice"),
            n => n.LastName.ShouldBe("Doe"));
    }

    // Given a ticket type with waitlisting enabled that has never gone into waitlist mode
    // When the waitlist details are retrieved
    // Then the result reports the waitlist as enabled with no entries, instead of an error
    [TestMethod]
    public async ValueTask HandleAsync_WaitlistEnabledWithNoEntries_ReturnsEnabledWithEmptyLists()
    {
        // Arrange
        var fixture = GetWaitlistDetailsFixture.WithWaitlistEnabledAndNoEntries();
        await fixture.SetupAsync(Environment);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(QueryFor(fixture), testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.WaitlistEnabled.ShouldBeTrue();
        result.ActiveEntries.ShouldBeEmpty();
        result.PendingNotifications.ShouldBeEmpty();
    }

    // Given a ticket type that has never had waitlisting enabled
    // When the waitlist details are retrieved
    // Then the result reports the waitlist as not enabled, instead of a 404-style error
    [TestMethod]
    public async ValueTask HandleAsync_WaitlistNotEnabled_ReturnsWaitlistEnabledFalse()
    {
        // Arrange
        var fixture = GetWaitlistDetailsFixture.WithWaitlistNotEnabled();
        await fixture.SetupAsync(Environment);
        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(QueryFor(fixture), testContext.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.WaitlistEnabled.ShouldBeFalse();
        result.ActiveEntries.ShouldBeEmpty();
        result.PendingNotifications.ShouldBeEmpty();
    }
}
