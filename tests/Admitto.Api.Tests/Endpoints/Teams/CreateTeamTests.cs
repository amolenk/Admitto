using Amolenk.Admitto.Application.Tests.Infrastructure;
using Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;

namespace Amolenk.Admitto.Application.Tests.Endpoints.Teams;

[TestClass]
public class CreateTeamTests
{
    [TestMethod]
    public async Task NameIsEmpty_ReturnsValidationProblem()
    {
        // Arrange
        var request = CreateRequest(name: string.Empty);
        var httpClient = GlobalAppHostFixture.Application.CreateHttpClient("api");
    
        // Act
        var response = await httpClient.PostAsJsonAsync($"/teams/", request);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await response.ShouldHaveProblemDetail(
            pd => pd.Errors.ShouldContainKey("name"));
    }
    
    [TestMethod]
    public async Task NameIsTooLong_ReturnsValidationProblem()
    {
        // Arrange
        var request = CreateRequest(name: "F".PadRight(101, 'o'));
        var httpClient = GlobalAppHostFixture.Application.CreateHttpClient("api");

        // Act
        var response = await httpClient.PostAsJsonAsync($"/teams/", request);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await response.ShouldHaveProblemDetail(
            pd => pd.Errors.ShouldContainKey("name"));
    }

    [TestClass]
    [DoNotParallelize]
    public class DatabaseTests
    {
        private static DatabaseFixture _databaseFixture = null!;

        [TestInitialize]
        public async Task TestInitialize()
        {
            _databaseFixture = await GlobalAppHostFixture.GetDatabaseFixtureAsync();
            await _databaseFixture.ResetAsync();
            await _databaseFixture.SeedDataAsync(context =>
            {
                var team = TestDataBuilder.CreateTeam(name: "Default Team");
                context.Teams.Add(team);
            });
        }
        
        [TestMethod]
        public async Task ValidTeam_CreatesTeam()
        {
            // Arrange
            var request = CreateRequest();
            var httpClient = GlobalAppHostFixture.Application.CreateHttpClient("api");

            // Act
            var response = await httpClient.PostAsJsonAsync($"/teams/", request);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Created);

            var result = (await response.Content.ReadFromJsonAsync<CreateTeamResponse>())!;

            result.ShouldSatisfyAllConditions(r => ((Guid?)r.Id).ShouldNotBeNull());
        }
        
        [TestMethod]
        public async Task TeamAlreadyExists_ReturnsValidationProblem()
        {
            // Arrange
            var request = CreateRequest(name: "Default Team");
            var httpClient = GlobalAppHostFixture.Application.CreateHttpClient("api");

            // Act
            var response = await httpClient.PostAsJsonAsync($"/teams/", request);
        
            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            await response.ShouldHaveProblemDetail(
                pd => pd.Detail.ShouldBe("A team with the name 'Default Team' already exists."),
                pd => pd.Errors.ShouldContainKey("name"));
        }
    }

    private static CreateTeamRequest CreateRequest(string? name = null)
    {
        name ??= "Test Team";
        
        return new CreateTeamRequest(name);
    }
}