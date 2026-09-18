using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.LookupCheckInCandidates;
using Shouldly;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.LookupCheckInCandidates;

[TestClass]
public sealed class CheckInLookupHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given registrations in the selected event and another event
    // When lookup is performed by exact registration id and partial name
    // Then exact lookup returns one candidate and partial lookup is scoped and bounded
    [TestMethod]
    public async ValueTask Lookup_ExactAndPartialQuery_ReturnsScopedMaximumTwentyCandidates()
    {
        var fixture = CheckInLookupFixture.Candidates();
        await fixture.SetupAsync(Environment);
        var sut = new LookupCheckInCandidatesHandler(Environment.RegistrationsDatabase.Context);

        var exact = await sut.HandleAsync(
            new LookupCheckInCandidatesQuery(
                fixture.TeamId.Value,
                fixture.EventId.Value,
                fixture.ExactRegistrationId.Value.ToString()),
            testContext.CancellationToken);
        var partial = await sut.HandleAsync(
            new LookupCheckInCandidatesQuery(fixture.TeamId.Value, fixture.EventId.Value, "  match  "),
            testContext.CancellationToken);

        exact.ShouldHaveSingleItem().RegistrationId.ShouldBe(fixture.ExactRegistrationId.Value);
        partial.Count.ShouldBe(20);
        partial.ShouldAllBe(candidate => candidate.Email.EndsWith("@example.com"));
        partial.ShouldNotContain(candidate => candidate.Name == "Match Other");
    }

    // Given registrations that can be searched by name or email
    // When lookup is performed with a one-character partial query
    // Then no candidates are returned
    [TestMethod]
    public async ValueTask Lookup_OneCharacterPartialQuery_ReturnsEmpty()
    {
        var fixture = CheckInLookupFixture.Candidates();
        await fixture.SetupAsync(Environment);
        var sut = new LookupCheckInCandidatesHandler(Environment.RegistrationsDatabase.Context);

        var result = await sut.HandleAsync(
            new LookupCheckInCandidatesQuery(fixture.TeamId.Value, fixture.EventId.Value, "m"),
            testContext.CancellationToken);

        result.ShouldBeEmpty();
    }

    // Given registered, checked-in, and cancelled lookup candidates
    // When lookup is performed by their shared name
    // Then each candidate exposes its current check-in state and timestamp
    [TestMethod]
    public async ValueTask Lookup_MixedRegistrationStates_ReturnsStateAndTimestamp()
    {
        var fixture = CheckInLookupFixture.Candidates();
        await fixture.SetupAsync(Environment);
        var sut = new LookupCheckInCandidatesHandler(Environment.RegistrationsDatabase.Context);

        var result = await sut.HandleAsync(
            new LookupCheckInCandidatesQuery(fixture.TeamId.Value, fixture.EventId.Value, "match"),
            testContext.CancellationToken);

        result.Any(candidate => candidate.State == CheckInCandidateState.CheckedIn && candidate.CheckedInAt is not null).ShouldBeTrue();
        result.Any(candidate => candidate.State == CheckInCandidateState.Cancelled && candidate.CheckedInAt is null).ShouldBeTrue();
        result.Any(candidate => candidate.State == CheckInCandidateState.Eligible && candidate.CheckedInAt is null).ShouldBeTrue();
    }
}
