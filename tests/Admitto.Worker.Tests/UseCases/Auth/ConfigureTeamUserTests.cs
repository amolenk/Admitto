// using Amolenk.Admitto.Domain.DomainEvents;
// using Amolenk.Admitto.Domain.Entities;
// using Amolenk.Admitto.Domain.ValueObjects;
// using Should = Amolenk.Admitto.TestHelpers.Should;
//
// namespace Amolenk.Admitto.Worker.Tests.UseCases.Auth;
//
// [TestClass]
// public class ConfigureTeamUserTests : WorkerTestsBase
// {
//     [TestMethod]
//     public async ValueTask TeamMemberAddedDomainEvent_ConfiguresTeamUser()
//     {
//         // Arrange
//         var teamMember = TeamMember.Create("alice@example.com", TeamMemberRole.Manager); // TODO Factory
//         var domainEvent = new TeamMemberAddedDomainEvent(DefaultTeam.Id, teamMember);
//         
//         // Act
//         await PublishDomainEventAsync(domainEvent);
//         
//         // Assert
//         Should.Eventually(() =>
//         {
//             // var teamUser = DefaultTeam.GetUser(teamMember.Email);
//             // teamUser.Should.NotBeNull();
//             // teamUser.Role.Should.Be(TeamMemberRole.Manager);
//         });
//     }
// }
