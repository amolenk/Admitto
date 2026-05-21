using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.PolicyConstraints;

[TestClass]
public sealed class PolicyConstraintsTests(TestContext testContext) : EndToEndTestBase
{
    // Registration window closing on or before event end is accepted
    [TestMethod]
    public async Task ConfigureRegistrationPolicy_WindowClosesAtEventEnd_Returns204()
    {
        var fixture = PolicyConstraintsFixture.WithActiveEvent();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.RegistrationPolicyRoute,
            new
            {
                OpensAt = PolicyConstraintsFixture.EventStartsAt.AddDays(-30),
                ClosesAt = PolicyConstraintsFixture.EventEndsAt,
            },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // Registration window closing after event end is rejected
    [TestMethod]
    public async Task ConfigureRegistrationPolicy_WindowClosesAfterEventEnd_Returns400()
    {
        var fixture = PolicyConstraintsFixture.WithActiveEvent();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.RegistrationPolicyRoute,
            new
            {
                OpensAt = PolicyConstraintsFixture.EventStartsAt.AddDays(-30),
                ClosesAt = PolicyConstraintsFixture.EventEndsAt.AddSeconds(1),
            },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Reconfirm window closing before event start is accepted
    [TestMethod]
    public async Task ConfigureReconfirmPolicy_WindowClosesBeforeEventStart_Returns204()
    {
        var fixture = PolicyConstraintsFixture.WithActiveEvent();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.ReconfirmPolicyRoute,
            new
            {
                OpensAt = PolicyConstraintsFixture.EventStartsAt.AddDays(-30),
                ClosesAt = PolicyConstraintsFixture.EventStartsAt.AddSeconds(-1),
                CadenceHours = 24,
                MinEmailIntervalHours = 1,
            },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    // Reconfirm window closing at event start is rejected
    [TestMethod]
    public async Task ConfigureReconfirmPolicy_WindowClosesAtEventStart_Returns400()
    {
        var fixture = PolicyConstraintsFixture.WithActiveEvent();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.ReconfirmPolicyRoute,
            new
            {
                OpensAt = PolicyConstraintsFixture.EventStartsAt.AddDays(-30),
                ClosesAt = PolicyConstraintsFixture.EventStartsAt,
                CadenceHours = 24,
                MinEmailIntervalHours = 1,
            },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // Reconfirm window closing after event start is rejected
    [TestMethod]
    public async Task ConfigureReconfirmPolicy_WindowClosesAfterEventStart_Returns400()
    {
        var fixture = PolicyConstraintsFixture.WithActiveEvent();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.ReconfirmPolicyRoute,
            new
            {
                OpensAt = PolicyConstraintsFixture.EventStartsAt.AddDays(-30),
                ClosesAt = PolicyConstraintsFixture.EventStartsAt.AddSeconds(1),
                CadenceHours = 24,
                MinEmailIntervalHours = 1,
            },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
