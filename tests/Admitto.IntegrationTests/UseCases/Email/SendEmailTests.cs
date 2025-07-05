// using Amolenk.Admitto.Application.UseCases.Email.SendEmail;
// using Amolenk.Admitto.Domain.Entities;
// using Amolenk.Admitto.Domain.ValueObjects;
// using Amolenk.Admitto.IntegrationTests.TestHelpers.Builders;
// using Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.ObjectModel;
//
// namespace Amolenk.Admitto.IntegrationTests.UseCases.Email;
//
// [TestClass]
// public class SendEmailTests : FullStackTestsBase
// {
//     private Team _testTeam = null!;
//         
//     [TestInitialize]
//     public override async Task TestInitialize()
//     {
//         await base.TestInitialize();
//
//         await SeedDatabaseAsync(context =>
//         {
//             _testTeam = new TeamBuilder()
//                 .WithEmailSettings(Email.DefaultEmailSettings)
//                 .Build();
//
//             context.Teams.Add(_testTeam);
//         });
//     }
//     
//     [TestMethod]
//     public async ValueTask EmailIsReadyToSend_SendsEmail()
//     {
//         // Arrange
//         var emailMessage = new EmailMessage
//         {
//             RecipientEmail = "attendee@example.com",
//             Subject = "Test",
//             Body = "Hello, world!",
//             TeamId = _testTeam.Id
//         };
//
//         await SeedDatabaseAsync(context =>
//         {
//             context.EmailMessages.Add(emailMessage);
//         });
//         
//         var command = new SendEmailCommand(emailMessage.Id);
//
//         // Act
//         await HandleCommand<SendEmailCommand, SendEmailHandler>(command);
//             
//         // Assert
//         await Email.ShouldHaveSentSingleEmailAsync(
//             email => email.To.Single().Address.ShouldBe(emailMessage.RecipientEmail),
//             email => email.Subject.ShouldBe(email.Subject),
//             email => email.Html.ShouldBe(emailMessage.Body + "\n"));
//     }
//
//     [TestMethod]
//     public async ValueTask EmailIsReadyForSend_MarksAsSendInDatabase()
//     {
//         // Arrange
//         var emailMessage = new EmailMessage
//         {
//             RecipientEmail = "attendee@example.com",
//             Subject = "Test",
//             Body = "Hello, world!",
//             TeamId = _testTeam.Id
//         };
//
//         await SeedDatabaseAsync(context =>
//         {
//             context.EmailMessages.Add(emailMessage);
//         });
//
//         var command = new SendEmailCommand(emailMessage.Id);
//
//         // Act
//         await HandleCommand<SendEmailCommand, SendEmailHandler>(command);
//         
//         // Assert
//         
//         // Reload the email message from the database to check its state.
//         emailMessage = await Database.Context.EmailMessages.FindAsync(emailMessage.Id);
//         
//         emailMessage.ShouldNotBeNull().IsSent.ShouldBeTrue();
//     }
//     
//     [TestMethod]
//     public async ValueTask EmailIsNotFound_DoesNotCrash()
//     {
//         // Arrange
//         var invalidEmailId = Guid.NewGuid();
//
//         var command = new SendEmailCommand(invalidEmailId);
//
//         // Act
//         await HandleCommand<SendEmailCommand, SendEmailHandler>(command);
//             
//         // Assert
//         await Email.ShouldNotHaveSentEmailAsync();
//     }
//     
//     [TestMethod]
//     public async ValueTask EmailIsAlreadySent_DoesNotSendTwice()
//     {
//         // Arrange
//         var emailMessage = new EmailMessage
//         {
//             RecipientEmail = "attendee@example.com",
//             Subject = "Test",
//             Body = "Hello, world!",
//             TeamId = _testTeam.Id
//         };
//
//         await SeedDatabaseAsync(context =>
//         {
//             context.EmailMessages.Add(emailMessage);
//         });
//         
//         var command = new SendEmailCommand(emailMessage.Id);
//         
//         // Act
//         await HandleCommand<SendEmailCommand, SendEmailHandler>(command);
//         await HandleCommand<SendEmailCommand, SendEmailHandler>(command);
//             
//         // Assert
//         await Email.ShouldHaveSentSingleEmailAsync();
//     }
// }
