using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Api.Tests.Infrastructure;
using Shouldly;

namespace Amolenk.Admitto.Api.Tests.Registrations.RegisterAttendee.PublicApi.SelfService;

[TestClass]
public sealed class SelfRegisterAttendeeTests(TestContext testContext) : EndToEndTestBase
{
    // NOTE: Cases (b) "valid token → 201" and (c) "invalid token → 400" are not exercised at the
    // API layer because the test host runs the real out-of-process API and currently has no way
    // to substitute the placeholder NotImplementedEmailVerificationTokenValidator with a fake.
    // Those cases are covered by the in-module integration tests under
    // Admitto.Core.Module.Registrations.Tests/.../RegisterAttendee/SelfRegisterAttendeeTests.cs which
    // instantiate the handler directly with a StubEmailVerificationTokenValidator.

    [TestMethod]
    public async Task SelfRegister_WithoutToken_Returns401EmailVerificationRequired()
    {
        var fixture = SelfRegisterAttendeeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var request = new
        {
            FirstName = "Alice",
            LastName = "Anderson",
            Email = "alice@example.com",
            RegisterTicketTypeIds = new[] { SelfRegisterAttendeeFixture.TicketTypeId.Value },
            WaitlistTicketTypeIds = Array.Empty<Guid>(),
            AdditionalDetails = new Dictionary<string, string>()
        };

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task SelfRegister_MissingExplicitTicketSets_Returns400ValidationProblem()
    {
        var fixture = SelfRegisterAttendeeFixture.HappyFlow();
        await fixture.SetupAsync(Environment);

        var request = new
        {
            FirstName = "Alice",
            LastName = "Anderson",
            Email = "alice@example.com",
            AdditionalDetails = new Dictionary<string, string>()
        };

        using var client = Environment.CreatePartnerApiClient(fixture.ApiKey);
        var response = await client.PostAsJsonAsync(
            fixture.Route, request, cancellationToken: testContext.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
