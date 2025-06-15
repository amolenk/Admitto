using System.Net;
using System.Net.Http.Json;
using Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.Utilities;
using Amolenk.Admitto.Domain.ValueObjects;
using Amolenk.Admitto.IntegrationTests.TestHelpers;
using Azure.Messaging;
using Should = Amolenk.Admitto.IntegrationTests.TestHelpers.Should;

namespace Amolenk.Admitto.IntegrationTests.UseCases.Teams;

[TestClass]
public class CreateTeamTests : BaseForApiTests
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
            EmailSettingsDto.FromEmailSettings(Email.DefaultEmailSettings),
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
    
    [DoNotParallelize]
    [TestClass]
    public class FullStackTests : BaseForFullStackTests
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

            var result = await response.Content.ReadFromJsonAsync<CreateTeamResponse>();

            (result?.Id).ShouldNotBeNull();
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
        public async Task NewTeamMember_PublishesTeamMemberAddedDomainEvent()
        {
            // Arrange
            const string teamName = "New Team";
            const string email = "bob@example.com";
            var request = CreateRequest(name: teamName,
                members: [new TeamMemberDto(email, TeamMemberRole.Manager)]);

            // Act
            var response = await ApiClient.PostAsJsonAsync($"/teams/", request);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.Created);

            // Check if the domain event was published
            await QueueStorage.MessageQueue.ShouldContainMessageAsync<TeamMemberAddedDomainEvent>(domainEvent =>
            {
                domainEvent.TeamId.ShouldBe(TeamId.FromName(teamName).Value);
                domainEvent.Member.Email.ShouldBe(email);
                domainEvent.Member.Role.ShouldBe<TeamMemberRole>(TeamMemberRole.Manager);
            });
        }
    }

    private static CreateTeamRequest CreateRequest(string? name = null, EmailSettingsDto? emailSettings = null,
        IEnumerable<TeamMemberDto>? members = null)
    {
        name ??= "Test Team";
        emailSettings ??= EmailSettingsDto.FromEmailSettings(AssemblyTestFixture.EmailTestFixture.DefaultEmailSettings);
        
        return new CreateTeamRequest(name, emailSettings, members ?? []);
    }

    // TODO Make Should extension
    private async ValueTask AssertReturnsBadRequestAsync(CreateTeamRequest request, string expectedErrorKey)
    {
        var response = await ApiClient.PostAsJsonAsync($"/teams/", request);
        
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await response.ShouldHaveProblemDetail(
            pd => pd.Errors.ShouldContainKey(expectedErrorKey));
    }
}
