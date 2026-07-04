using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.PolicyConstraints;

[TestClass]
public sealed class PolicyConstraintsTests(TestContext testContext) : EndToEndTestBase
{
    [TestMethod]
    public async Task ConfigureRegistrationPolicy_WindowClosesAtEventStart_Returns204()
    {
        var fixture = PolicyConstraintsFixture.WithActiveEvent();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.RegistrationPolicyRoute,
            new
            {
                OpensAt = PolicyConstraintsFixture.EventStartsAt.AddDays(-30),
                ClosesAt = PolicyConstraintsFixture.EventStartsAt,
            },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task ConfigureRegistrationPolicy_WindowClosesAfterEventStart_Returns400()
    {
        var fixture = PolicyConstraintsFixture.WithActiveEvent();
        await fixture.SetupAsync(Environment);

        var response = await Environment.ApiClient.PutAsJsonAsync(
            fixture.RegistrationPolicyRoute,
            new
            {
                OpensAt = PolicyConstraintsFixture.EventStartsAt.AddDays(-30),
                ClosesAt = PolicyConstraintsFixture.EventStartsAt.AddSeconds(1),
            },
            cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

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
