using System.Net;
using System.Text.Json;
using Amolenk.Admitto.Core.Organization.Infrastructure.UserDirectories.Keycloak;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.IntegrationTests.Organization.Infrastructure.UserDirectories.Keycloak;

[TestClass]
public sealed class KeycloakUserManagementServiceTests
{
    // Given a Keycloak user that already exists for the given email
    // When the user is invited
    // Then no new user is created, an enrollment email is sent, and the existing user id is returned
    [TestMethod]
    public async Task InviteUserAsync_ExistingUser_SendsEnrollmentEmailAndReturnsUserId()
    {
        var handler = new RecordingHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
                return JsonResponse("[{\"id\":\"existing-user-id\"}]");

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        var options = new KeycloakOptions
        {
            ExecuteActionsClientId = "admitto-ui",
            ExecuteActionsRedirectUri = "http://localhost"
        };

        var sut = new KeycloakUserManagementService(NewClient(handler), Options.Create(options));

        var result = await sut.InviteUserAsync("admin@example.com");

        result.ShouldBe("existing-user-id");
        handler.Requests.Count(r => r.Method == HttpMethod.Put && r.RequestUri!.AbsolutePath.EndsWith("/execute-actions-email")).ShouldBe(1);
        await AssertExecuteActionsEmailAsync(handler);
    }

    // Given no Keycloak user exists for the given email
    // When the user is invited
    // Then a new passwordless webauthn user is created and an enrollment email is sent
    [TestMethod]
    public async Task InviteUserAsync_NewUser_CreatesUserAndSendsEnrollmentEmail()
    {
        var handler = new RecordingHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
                return JsonResponse("[]");

            if (request.Method == HttpMethod.Post)
            {
                var response = new HttpResponseMessage(HttpStatusCode.Created);
                response.Headers.Location = new Uri("http://keycloak/admin/realms/admitto/users/new-user-id");
                return response;
            }

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });

        var options = new KeycloakOptions
        {
            ExecuteActionsClientId = "admitto-ui",
            ExecuteActionsRedirectUri = "http://localhost"
        };

        var sut = new KeycloakUserManagementService(NewClient(handler), Options.Create(options));

        var result = await sut.InviteUserAsync("admin@example.com");

        result.ShouldBe("new-user-id");
        handler.Requests.Count(r => r.Method == HttpMethod.Post && r.RequestUri!.AbsolutePath.EndsWith("/users")).ShouldBe(1);
        handler.Requests.Count(r => r.Method == HttpMethod.Put && r.RequestUri!.AbsolutePath.EndsWith("/execute-actions-email")).ShouldBe(1);
        await AssertExecuteActionsEmailAsync(handler);

        var createBody = await handler.Requests.Single(r => r.Method == HttpMethod.Post).Content!.ReadAsStringAsync();
        createBody.ShouldContain("webauthn-register-passwordless");
    }

    // Given execute-actions client id and redirect URI configured in Keycloak options
    // When the user is invited
    // Then the execute-actions-email request includes the client id and redirect URI as query parameters
    [TestMethod]
    public async Task InviteUserAsync_WithExecuteActionsClient_AppendsClientAndRedirectUri()
    {
        var handler = new RecordingHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
                return JsonResponse("[{\"id\":\"existing-user-id\"}]");

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var options = Options.Create(new KeycloakOptions
        {
            ExecuteActionsClientId = "admitto-ui",
            ExecuteActionsRedirectUri = "http://localhost:3000"
        });
        var sut = new KeycloakUserManagementService(NewClient(handler), options);

        await sut.InviteUserAsync("admin@example.com");

        var request = handler.Requests.Single(r =>
            r.Method == HttpMethod.Put && r.RequestUri!.AbsolutePath.EndsWith("/execute-actions-email"));
        request.RequestUri!.Query.ShouldContain("client_id=admitto-ui");
        request.RequestUri!.Query.ShouldContain("redirect_uri=http%3A%2F%2Flocalhost%3A3000");
    }

    private static async Task AssertExecuteActionsEmailAsync(RecordingHandler handler)
    {
        var request = handler.Requests.Single(r =>
            r.Method == HttpMethod.Put && r.RequestUri!.AbsolutePath.EndsWith("/execute-actions-email"));
        var body = await request.Content!.ReadAsStringAsync();
        var actions = JsonSerializer.Deserialize<string[]>(body);

        actions.ShouldBe(["webauthn-register-passwordless"]);
    }

    private static HttpClient NewClient(HttpMessageHandler handler) => new(handler)
    {
        BaseAddress = new Uri("http://keycloak")
    };

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
    };

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }
}
