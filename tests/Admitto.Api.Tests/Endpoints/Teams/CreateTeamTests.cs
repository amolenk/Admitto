using Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;
using Amolenk.Admitto.Domain.ValueObjects;
using Should = Amolenk.Admitto.Application.Tests.TestHelpers.Should;

namespace Amolenk.Admitto.Application.Tests.Endpoints.Teams;

[TestClass]
public class CreateTeamTests : BasicApiTestsBase
{
    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("X")] // Name is too short
    [DataRow("012345678901234567890123456789012345678901234567891")] // Name is too long
    public async Task NameIsInvalid_ReturnsBadRequest(string? name)
    {
        // Arrange
        var request = new CreateTeamRequest(
            name!,
            EmailSettingsDto.FromEmailSettings(GlobalAppHostFixture.GetDefaultEmailSettings()),
            []);
    
        // Act & Assert
        await AssertReturnsBadRequestAsync(request, "name");
    }
    
    [TestMethod]
    public async Task EmailSettingsAreMissing_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateTeamRequest("test", null!, []);
        
        // Act & Assert
        await AssertReturnsBadRequestAsync(request, "emailSettings");
    }

    [DataTestMethod]
    [DataRow(null, "smtp.example.com", 25, "senderEmail")] // EmailSender is null
    [DataRow("not-an-email", "smtp.example.com", 25, "senderEmail")] // EmailSender is not a valid email address
    [DataRow("alice@example.com", null, 25, "smtpServer")] // SmtpServer is null
    [DataRow("alice@example.com", "", 25, "smtpServer")] // SmtpServer is empty
    [DataRow("alice@example.com", "012345678901234567891", 25, "smtpServer")] // SmtpServer is too long
    [DataRow("alice@example.com", "smtp.example.com", 0, "smtpPort")] // SmtpPort is too low
    [DataRow("alice@example.com", "smtp.example.com", 65536, "smtpPort")] // SmtpPort is too high
    public async Task EmailSettingsAreInvalid_ReturnsBadRequest(string? senderEmail, string? smtpServer, int smtpPort,
        string expectedErrorKey)
    {
        // Arrange
        var emailSettings = new EmailSettingsDto(senderEmail!, smtpServer!, smtpPort);
        var request = CreateRequest(emailSettings: emailSettings);
        
        // Act & Assert
        await AssertReturnsBadRequestAsync(request, expectedErrorKey);
    }
    
    [DataTestMethod]
    [DataRow(null, TeamMemberRole.Manager, "email")] // Email is null
    [DataRow("not-an-email", TeamMemberRole.Manager, "email")] // Email is not a valid email address
    [DataRow("alice@example.com", null, "role")] // Role is null
    [DataRow("alice@example.com", "", "role")] // Role is empty
    [DataRow("alice@example.com", "GrandWizard", "role")] // Role is unknown
    public async Task MemberIsInvalid_ReturnsBadRequest(string email, string role, string expectedErrorKey)
    {
        // Arrange
        var teamMember = new TeamMemberDto(email, role);
        var request = CreateRequest(members: [teamMember]);
        
        // Act & Assert
        await AssertReturnsBadRequestAsync(request, $"members[0].{expectedErrorKey}");
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

    private static CreateTeamRequest CreateRequest(string? name = null, EmailSettingsDto? emailSettings = null,
        IEnumerable<TeamMemberDto>? members = null)
    {
        name ??= "Test Team";
        emailSettings ??= EmailSettingsDto.FromEmailSettings(GlobalAppHostFixture.GetDefaultEmailSettings());
        
        return new CreateTeamRequest(name, emailSettings, members ?? []);
    }

    private async ValueTask AssertReturnsBadRequestAsync(CreateTeamRequest request, string expectedErrorKey)
    {
        var response = await ApiClient.PostAsJsonAsync($"/teams/", request);
        
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await response.ShouldHaveProblemDetail(
            pd => pd.Errors.ShouldContainKey(expectedErrorKey));
    }
}
