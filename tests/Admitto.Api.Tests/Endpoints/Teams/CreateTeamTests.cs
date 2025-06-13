using Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Should = Amolenk.Admitto.Application.Tests.TestHelpers.Should;

namespace Amolenk.Admitto.Application.Tests.Endpoints.Teams;

[TestClass]
public class CreateTeamTests
{
    [TestMethod]
    public async Task NameIsTooShort_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateRequest(name: "X");
        var httpClient = GlobalAppHostFixture.GetApiClient();
    
        // Act
        var response = await httpClient.PostAsJsonAsync($"/teams/", request);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await response.ShouldHaveProblemDetail(
            pd => pd.Errors.ShouldContainKey("name"));
    }
    
    [TestMethod]
    public async Task NameIsTooLong_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateRequest(name: "F".PadRight(101, 'o'));
        var httpClient = GlobalAppHostFixture.GetApiClient();

        // Act
        var response = await httpClient.PostAsJsonAsync($"/teams/", request);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await response.ShouldHaveProblemDetail(
            pd => pd.Errors.ShouldContainKey("name"));
    }

    [TestClass]
    [DoNotParallelize]
    public class SequentialTests
    {
        private static DatabaseFixture _databaseFixture = null!;
        private static IdentityFixture _identityFixture = null!;
        private static AuthorizationFixture _authorizationFixture = null!;
        private static QueueStorageFixture _queueStorageFixture = null!;

        private Team _defaultTeam = null!;
        
        [TestInitialize]
        public async Task TestInitialize()
        {
            _databaseFixture = await GlobalAppHostFixture.GetDatabaseFixtureAsync();
            _identityFixture = GlobalAppHostFixture.GetIdentityFixture();
            _authorizationFixture = GlobalAppHostFixture.GetAuthorizationFixture();
            _queueStorageFixture = await GlobalAppHostFixture.GetQueueStorageFixtureAsync();

            await Task.WhenAll(
                _databaseFixture.ResetAsync(context =>
                {
                    _defaultTeam = TeamDataFactory.CreateTeam(name: "Default Team");
                    context.Teams.Add(_defaultTeam);
                }),
                _identityFixture.ResetAsync(),
                _authorizationFixture.ResetAsync(),
                _queueStorageFixture.ResetAsync());
        }

        [TestMethod]
        public async Task TestMail()
        {
            
        }
        
        [TestMethod]
        public async Task ValidTeam_CreatesTeam()
        {
            // Arrange
            var request = CreateRequest();
            var httpClient = GlobalAppHostFixture.GetApiClient();

            // Act
            var response = await httpClient.PostAsJsonAsync($"/teams/", request);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Created);

            var result = (await response.Content.ReadFromJsonAsync<CreateTeamResponse>())!;

            result.ShouldSatisfyAllConditions(r => ((Guid?)r.Id).ShouldNotBeNull());
        }
        
        [TestMethod]
        public async Task TeamAlreadyExists_ReturnsConflict()
        {
            // Arrange
            var request = CreateRequest(name: _defaultTeam.Name);
            var httpClient = GlobalAppHostFixture.GetApiClient();

            // Act
            var response = await httpClient.PostAsJsonAsync($"/teams/", request);
        
            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            await response.ShouldHaveProblemDetail(
                pd => pd.Detail.ShouldBe($"A team with the name '{_defaultTeam.Name}' already exists."),
                pd => pd.Errors.ShouldContainKey("name"));
        }

        [TestMethod]
        public async Task NewTeamMember_CreatesUser()
        {
            // Arrange
            const string email = "bob@example.com";
            var request = CreateRequest(members: [new TeamMemberDto(email, TeamMemberRole.Manager)]);
            var httpClient = GlobalAppHostFixture.GetApiClient();

            // Act
            var response = await httpClient.PostAsJsonAsync($"/teams/", request);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Created);

            // Check if the user was created
            await Should.Eventually(async () =>
                {
                    var user = await _identityFixture.IdentityService.GetUserByEmailAsync(email);
                    user.ShouldNotBeNull().Email.ShouldBe(email);
                },
                TimeSpan.FromSeconds(3));
        }

        [TestMethod]
        public async Task TeamMemberAlreadyExists_DoesNotCreateExtraUser()
        {
            // Arrange
            const string email = "bob@example.com";
            var request = CreateRequest(members: [new TeamMemberDto(email, TeamMemberRole.Manager)]);
            var httpClient = GlobalAppHostFixture.GetApiClient();

            // Ensure the user already exists.
            await _identityFixture.IdentityService.AddUserAsync(email);

            // Act
            var response = await httpClient.PostAsJsonAsync($"/teams/", request);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Created);

            // Check that no extra user was created
            await Should.Eventually(async () =>
                {
                    var users = await _identityFixture.IdentityService.GetUsersAsync();
                    users.Count().ShouldBe(2, "Only Alice and Bob should be returned.");
                },
                TimeSpan.FromSeconds(3));
        }
    }

    private static CreateTeamRequest CreateRequest(string? name = null, IEnumerable<TeamMemberDto>? members = null)
    {
        name ??= "Test Team";

        return new CreateTeamRequest(
            name, 
            EmailSettingsDto.FromEmailSettings(GlobalAppHostFixture.GetDefaultEmailSettings()),
            members ?? []);
    }
}