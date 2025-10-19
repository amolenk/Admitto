using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.IntegrationTests.TestHelpers.Builders;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.IntegrationTests.UseCases.Teams;

[TestClass]
public class CreateTeamTests : ApiTestsBase
{
    private const string RequestUri = "/teams";

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("x")] // Too short
    [DataRow("012345678901234567890123456789012345678901234567891")] // Too long
    [DataRow("NotKebabCase")]
    public async Task SlugIsInvalid_ReturnsBadRequest(string? slug)
    {
        // Arrange
        var request = new CreateTeamRequestBuilder()
            .WithSlug(slug!)
            .Build();
        
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldBeBadRequestForFieldAsync("slug");
    }
    
    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("x")] // Too short
    [DataRow("012345678901234567890123456789012345678901234567891")] // Too long
    public async Task NameIsInvalid_ReturnsBadRequest(string? name)
    {
        // Arrange
        var request = new CreateTeamRequestBuilder()
            .WithName(name!)
            .Build();
        
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldBeBadRequestForFieldAsync("name");
    }
    
    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("x")] // Too short
    [DataRow("012345678901234567890123456789012345678901234567891")] // Too long
    [DataRow("not-an-email")]
    public async Task EmailIsInvalid_ReturnsBadRequest(string? email)
    {
        // Arrange
        var request = new CreateTeamRequestBuilder()
            .WithEmail(email!)
            .Build();

        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldBeBadRequestForFieldAsync("email");
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("x")] // Too short
    [DataRow("01234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567891")] // Too long
    public async Task EmailSettingsAreInvalid_ReturnsBadRequest(string? emailServiceConnectionString)
    {
        // Arrange
        var request = new CreateTeamRequestBuilder()
            .WithEmailServiceConnectionString(emailServiceConnectionString!)
            .Build();
        
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldBeBadRequestForFieldAsync("emailServiceConnectionString");
    }
    
    [DoNotParallelize]
    [TestClass]
    public class FullStackTests : FullStackTestsBase
    {
        [TestMethod]
        public async Task ValidTeam_CreatesTeam()
        {
            // Arrange
            var request = new CreateTeamRequestBuilder()
                .Build();
        
            // Act
            var response = await ApiClient.PostAsJsonAsync(RequestUri, request);
        
            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Created);
            
            var createdTeam = await Database.Context.Teams.FirstOrDefaultAsync(t => t.Slug == request.Slug);
            createdTeam.ShouldNotBeNull();
        }
        
        [TestMethod]
        public async Task TeamWithSameSlugAlreadyExists_ReturnsBadRequest()
        {
            // Arrange
        
            // Ensure the team already exists
            await ApiClient.PostAsJsonAsync(RequestUri, new CreateTeamRequestBuilder()
                .Build());
            
            // Act
            var response = await ApiClient.PostAsJsonAsync(RequestUri, new CreateTeamRequestBuilder()
                .WithName("Different Name")
                .Build());
                
            // Assert
            await response.ShouldBeBadRequestAsync(ApplicationRuleError.Team.AlreadyExists);
        }
        
        [TestMethod]
        public async Task TeamWithSameNameAlreadyExists_ReturnsBadRequest()
        {
            // Arrange
        
            // Ensure the team already exists
            await ApiClient.PostAsJsonAsync(RequestUri, new CreateTeamRequestBuilder()
                .Build());
            
            // Act
            var response = await ApiClient.PostAsJsonAsync(RequestUri, new CreateTeamRequestBuilder()
                .WithSlug("different-slug")
                .Build());
                
            // Assert
            await response.ShouldBeBadRequestAsync(ApplicationRuleError.Team.AlreadyExists);
        }
    }
}
