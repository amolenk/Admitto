using Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;

namespace Amolenk.Admitto.Application.Tests.Endpoints.Teams;

[TestClass]
public class CreateTeamTests : DistributedAppTestBase
{
    [TestMethod]
    public async Task CreateTeam_NameIsEmpty_ReturnsValidationProblem()
    {
        // Arrange
        var request = CreateRequest(name: string.Empty);
    
        // Act
        var response = await Api.PostAsJsonAsync($"/teams/", request);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await response.ShouldHaveProblemDetail(nameof(request.Name));
    }
    
    [TestMethod]
    public async Task CreateTeam_NameIsTooLong_ReturnsValidationProblem()
    {
        // Arrange
        var request = CreateRequest(name: "F".PadRight(101, 'o'));

        // Act
        var response = await Api.PostAsJsonAsync($"/teams/", request);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await response.ShouldHaveProblemDetail(nameof(request.Name));
    }
    
    [TestMethod, DoNotParallelize]
    public async Task CreateTeam_TeamAlreadyExists_ReturnsValidationProblem()
    {
        // Arrange
        var request = CreateRequest(name: "Default Team");

        // Act
        var response = await Api.PostAsJsonAsync($"/teams/", request);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await response.ShouldHaveProblemDetail(
            pd => pd.Detail.ShouldBe("A team with the name 'Default Team' already exists."),
            pd => pd.Errors.ShouldContainKey("Name"));
    }
    
    [TestMethod, DoNotParallelize]
    public async Task CreateTeam_ValidTeam_CreatesTeam()
    {
        // Arrange
        var request = CreateRequest();

        // Act
        var response = await Api.PostAsJsonAsync($"/teams/", request);
               
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        var result = (await response.Content.ReadFromJsonAsync<CreateTeamResponse>())!;
        
        result.ShouldSatisfyAllConditions(r => ((Guid?)r.Id).ShouldNotBeNull());
    }

    private static CreateTeamRequest CreateRequest(string? name = null)
    {
        name ??= "Test Team";
        
        return new CreateTeamRequest(name);
    }
}