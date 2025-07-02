using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.UseCases.Teams.CreateTeam;
using Amolenk.Admitto.Domain;
using Amolenk.Admitto.Domain.DomainEvents;
using Amolenk.Admitto.Domain.ValueObjects;
using Amolenk.Admitto.IntegrationTests.TestHelpers.Builders;

namespace Amolenk.Admitto.IntegrationTests.UseCases.Teams;

[TestClass]
public class CreateTeamTests : ApiTestsBase
{
    // TODO Use builder pattern for request creation
    
    private const string RequestUri = "/teams/v1";
    
    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("X")] // Name is too short
    [DataRow("012345678901234567890123456789012345678901234567891")] // Name is too long
    public async Task NameIsInvalid_ReturnsBadRequest(string? name)
    {
        // Arrange
        var request = new CreateTeamRequestBuilder()
            .WithName(name!)
            .Build();
        
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldHaveProblemDetailAsync(
            conditions: pd => pd.Errors.ShouldContainKey("name"));
    }
    
    [TestMethod]
    public async Task EmailSettingsAreMissing_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateTeamRequestBuilder()
            .WithEmailSettings(null!)
            .Build();

        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldHaveProblemDetailAsync(
            conditions: pd => pd.Errors.ShouldContainKey("emailSettings"));
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
        var request = new CreateTeamRequestBuilder()
            .WithEmailSettings(emailSettings)
            .Build();
        
        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldHaveProblemDetailAsync(
            conditions: pd => pd.Errors.ShouldContainKey(expectedErrorKey));
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
        var request = new CreateTeamRequestBuilder()
            .WithMembers([teamMember])
            .Build();

        // Act
        var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

        // Assert
        await response.ShouldHaveProblemDetailAsync(
            conditions: pd => pd.Errors.ShouldContainKey($"members[0].{expectedErrorKey}"));
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

            var result = await response.Content.ReadFromJsonAsync<CreateTeamResponse>();
            (result?.Id).ShouldNotBeNull();
            
            var createdTeam = await Database.Context.Teams.FindAsync(result.Id);
            createdTeam.ShouldNotBeNull();

        }
        
        [TestMethod]
        public async Task TeamAlreadyExists_ReturnsConflict()
        {
            // Arrange
            var existingTeam = new TeamBuilder()
                .WithName("Existing Team")
                .Build();

            await SeedDatabaseAsync(context =>
            {
                context.Teams.Add(existingTeam);
            });
            
            var request = new CreateTeamRequestBuilder()
                .WithName(existingTeam.Name)
                .Build();

            // Ensure the team already exists
            await ApiClient.PostAsJsonAsync(RequestUri, request);
                
            // Act
            var response = await ApiClient.PostAsJsonAsync(RequestUri, request);
        
            // Assert
            await response.ShouldHaveProblemDetailAsync(
                HttpStatusCode.Conflict,
                ErrorMessage.Team.AlreadyExists,
                conditions: pd => pd.ShouldContainError(
                    nameof(request.Name), ErrorMessage.Team.Name.MustBeUnique));
        }

        [TestMethod]
        public async Task NewTeamMember_PublishesTeamMemberAddedDomainEvent()
        {
            // Arrange
            const string teamName = "New Team";
            const string email = "bob@example.com";
            
            var request = new CreateTeamRequestBuilder()
                .WithName(teamName)
                .WithMembers([new TeamMemberDto(email, TeamMemberRole.Manager)])
                .Build();

            // Act
            var response = await ApiClient.PostAsJsonAsync(RequestUri, request);

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
}
