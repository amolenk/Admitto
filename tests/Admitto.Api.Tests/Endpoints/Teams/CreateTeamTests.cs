using Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using Should = Amolenk.Admitto.Application.Tests.TestHelpers.Should;

namespace Amolenk.Admitto.Application.Tests.Endpoints.Teams;

[TestClass]
public class CreateTeamTests : BasicApiTestsBase
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
    public class FullStackTests : FullStackApiTestsBase
    {
        [TestMethod]
        public async Task ValidTeam_CreatesTeam()
        {
            // Arrange
            var request = CreateRequest();

            // Act
            var response = await ApiClient.PostAsJsonAsync($"/teams/", request);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Created);

            var result = (await response.Content.ReadFromJsonAsync<CreateTeamResponse>())!;

            result.ShouldSatisfyAllConditions(r => ((Guid?)r.Id).ShouldNotBeNull());
        }
        
        [TestMethod]
        public async Task TeamAlreadyExists_ReturnsConflict()
        {
            // Arrange
            var request = CreateRequest(name: DefaultTeam.Name);

            // Act
            var response = await ApiClient.PostAsJsonAsync($"/teams/", request);
        
            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            await response.ShouldHaveProblemDetail(
                pd => pd.Detail.ShouldBe($"A team with the name '{DefaultTeam.Name}' already exists."),
                pd => pd.Errors.ShouldContainKey("name"));
        }

        [TestMethod]
        public async Task NewTeamMember_CreatesUser()
        {
            // Arrange
            const string email = "bob@example.com";
            var request = CreateRequest(members: [new TeamMemberDto(email, TeamMemberRole.Manager)]);

            // Act
            var response = await ApiClient.PostAsJsonAsync($"/teams/", request);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Created);

            // Check if the user was created
            await Should.Eventually(async () =>
                {
                    var user = await IdentityFixture.IdentityService.GetUserByEmailAsync(email);
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

            // Ensure the user already exists.
            await IdentityFixture.IdentityService.AddUserAsync(email);

            // Act
            var response = await ApiClient.PostAsJsonAsync($"/teams/", request);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Created);

            // Check that no extra user was created
            await Should.Eventually(async () =>
                {
                    var users = await IdentityFixture.IdentityService.GetUsersAsync();
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
