using Amolenk.Admitto.Application.Tests.Infrastructure;
using Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;

namespace Amolenk.Admitto.Application.Tests.Middleware;

[TestClass]
public class ValidationExceptionHandlerTests
{
    [TestMethod]
    public async Task ValidationException_ReturnsProblemDetails()
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

    private static CreateTeamRequest CreateRequest(string? name = null)
    {
        name ??= "Test Team";
        
        return new CreateTeamRequest(name);
    }
}