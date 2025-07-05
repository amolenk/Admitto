// using Amolenk.Admitto.Application.Common.Abstractions;
// using Amolenk.Admitto.Application.Jobs.SendEmail;
// using Amolenk.Admitto.Domain.Entities;
// using Amolenk.Admitto.IntegrationTests.TestHelpers;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace Amolenk.Admitto.IntegrationTests.UseCases.Jobs;
//
// [TestClass]
// public class JobRunnerTests : ApiTestsBase
// {
//     [TestMethod]
//     public async Task StartJob_ShouldCreateJobInDatabase()
//     {
//         // Arrange
//         using var scope = Database.ServiceProvider.CreateScope();
//         var jobRunner = scope.ServiceProvider.GetRequiredService<IJobRunner>();
//         var jobContext = scope.ServiceProvider.GetRequiredService<IJobContext>();
//
//         var job = new SendEmailJob
//         {
//             RecipientEmail = "test@example.com",
//             Subject = "Test Subject",
//             Body = "Test Body"
//         };
//
//         // Act
//         await jobRunner.StartJob(job);
//
//         // Assert
//         var jobEntity = await jobContext.Jobs
//             .FirstOrDefaultAsync(j => j.Id == job.Id);
//
//         jobEntity.Should().NotBeNull();
//         jobEntity!.JobType.Should().Be("Amolenk.Admitto.Application.UseCases.Jobs.SendEmailJob");
//         jobEntity.Status.Should().Be(JobStatus.Pending);
//     }
//
//     [TestMethod]
//     public async Task AddOrUpdateScheduledJob_ShouldCreateScheduledJobInDatabase()
//     {
//         // Arrange
//         using var scope = Database.ServiceProvider.CreateScope();
//         var jobRunner = scope.ServiceProvider.GetRequiredService<IJobRunner>();
//         var jobContext = scope.ServiceProvider.GetRequiredService<IJobContext>();
//
//         var job = new PurgeExpiredRegistrationsJob
//         {
//             MaxExpireTime = TimeSpan.FromDays(30)
//         };
//
//         // Act
//         await jobRunner.AddOrUpdateScheduledJob(job, "0 0 * * *"); // Daily at midnight
//
//         // Assert
//         var scheduledJob = await jobContext.ScheduledJobs
//             .FirstOrDefaultAsync(sj => sj.Id == job.Id);
//
//         scheduledJob.Should().NotBeNull();
//         scheduledJob!.JobType.Should().Be("Amolenk.Admitto.Application.UseCases.Jobs.PurgeExpiredRegistrationsJob");
//         scheduledJob.CronExpression.Should().Be("0 0 * * *");
//         scheduledJob.IsEnabled.Should().BeTrue();
//     }
//
//     [TestMethod]
//     public async Task AddOrUpdateScheduledJob_WithInvalidCronExpression_ShouldThrow()
//     {
//         // Arrange
//         using var scope = Database.ServiceProvider.CreateScope();
//         var jobRunner = scope.ServiceProvider.GetRequiredService<IJobRunner>();
//
//         var job = new PurgeExpiredRegistrationsJob
//         {
//             MaxExpireTime = TimeSpan.FromDays(30)
//         };
//
//         // Act & Assert
//         await Assert.ThrowsExceptionAsync<ArgumentException>(
//             () => jobRunner.AddOrUpdateScheduledJob(job, "invalid cron").AsTask());
//     }
// }