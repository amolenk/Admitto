using Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;

namespace Amolenk.Admitto.Application.Tests.Middleware;

[TestClass]
public class DbExceptionHandlerTests : DistributedAppTestBase
{
    [TestMethod, DoNotParallelize]
    public async Task DbException_ReturnsProblemDetails()
    {
        // Arrange
        var request = CreateTeamRequest("Default Team");
        
        // Act
        var response = await Api.PostAsJsonAsync($"/teams", request);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await response.ShouldHaveProblemDetail(pd => 
            pd.Title.ShouldBe("Database update error."));
    }
    
    private static CreateTeamRequest CreateTeamRequest(string? name = null)
    {
        name ??= "Test Team";
        
        return new CreateTeamRequest(name);
    }
}