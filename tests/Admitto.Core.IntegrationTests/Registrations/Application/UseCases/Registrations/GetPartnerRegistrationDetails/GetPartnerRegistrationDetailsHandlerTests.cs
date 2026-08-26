using Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.GetRegistrationDetails;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetPartnerRegistrationDetails;
using Amolenk.Admitto.Core.Registrations.Contracts;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.GetPartnerRegistrationDetails;

[TestClass]
public sealed class GetPartnerRegistrationDetailsHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a registered attendee with additional details
    // When the partner registration details are queried
    // Then a reduced detail view including tickets and additional details is returned
    [TestMethod]
    public async ValueTask GetPartnerRegistrationDetails_ExistingRegistration_ReturnsReducedDetail()
    {
        var fixture = GetRegistrationDetailsFixture.WithAdditionalDetails();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetPartnerRegistrationDetailsQuery(
                fixture.TeamId.Value,
                fixture.EventId,
                fixture.RegistrationId.Value),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(fixture.RegistrationId.Value);
        result.Email.ShouldBe("alice@example.com");
        result.FirstName.ShouldBe("Alice");
        result.LastName.ShouldBe("Doe");
        result.Status.ShouldBe(RegistrationStatus.Registered);
        result.TicketTypeIds.ShouldHaveSingleItem().ShouldBe(fixture.TicketTypeId.Value);
        result.Tickets.ShouldHaveSingleItem().Id.ShouldBe(fixture.TicketTypeId.Value);
        result.AdditionalDetails["dietary"].ShouldBe("vegan");
    }

    // Given a registered attendee with no additional details
    // When the partner registration details are queried
    // Then the additional details collection is empty
    [TestMethod]
    public async ValueTask GetPartnerRegistrationDetails_NoAdditionalDetails_ReturnsEmptyDetails()
    {
        var fixture = GetRegistrationDetailsFixture.WithRegisteredAttendee();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetPartnerRegistrationDetailsQuery(
                fixture.TeamId.Value,
                fixture.EventId,
                fixture.RegistrationId.Value),
            testContext.CancellationToken);

        result.ShouldNotBeNull();
        result.AdditionalDetails.ShouldBeEmpty();
    }

    // Given no registration exists with the requested id
    // When the partner registration details are queried
    // Then the result is null
    [TestMethod]
    public async ValueTask GetPartnerRegistrationDetails_UnknownRegistration_ReturnsNull()
    {
        var fixture = GetRegistrationDetailsFixture.WithRegisteredAttendee();
        await fixture.SetupAsync(Environment);

        var result = await NewHandler().HandleAsync(
            new GetPartnerRegistrationDetailsQuery(
                fixture.TeamId.Value,
                fixture.EventId,
                Guid.NewGuid()),
            testContext.CancellationToken);

        result.ShouldBeNull();
    }

    private static GetPartnerRegistrationDetailsHandler NewHandler() =>
        new(Environment.RegistrationsDatabase.Context);
}
