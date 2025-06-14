using Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;
using Amolenk.Admitto.TestHelpers;

namespace Amolenk.Admitto.Api.Tests.Middleware;

[TestClass]
public class ValidationExceptionHandlerTests : ApiTestsBase
{
    [TestMethod]
    public async Task ValidationException_ReturnsProblemDetails()
    {
        // Arrange
        var request = CreateRequest(name: string.Empty);
    
        // Act
        var response = await ApiClient.PostAsJsonAsync($"/teams/", request);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await response.ShouldHaveProblemDetail(
            pd => pd.Errors.ShouldContainKey("name"));
    }

    private CreateTeamRequest CreateRequest(string? name = null)
    {
        name ??= "Test Team";
        
        return new CreateTeamRequest(
            name, 
            EmailSettingsDto.FromEmailSettings(Email.DefaultEmailSettings),
            []);
    }
}