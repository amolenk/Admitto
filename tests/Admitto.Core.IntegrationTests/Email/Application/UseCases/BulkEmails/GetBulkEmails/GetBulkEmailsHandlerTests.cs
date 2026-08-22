using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.GetBulkEmails;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.BulkEmails.GetBulkEmails;

[TestClass]
public sealed class GetBulkEmailsHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a bulk email job that exists for team A
    // When bulk emails are queried using team B's ID for the same event
    // Then an empty list is returned
    [TestMethod]
    public async ValueTask GetBulkEmails_WrongTeamId_ReturnsEmptyList()
    {
        // Arrange: create a bulk email job for team A
        var teamIdA = TeamId.New();
        var teamIdB = TeamId.New();
        var eventId = TicketedEventId.New();

        var job = BulkEmailJob.Create(
            teamId: teamIdA,
            ticketedEventId: eventId,
            emailType: "Confirmation",
            subject: "Your ticket",
            textBody: "Your ticket",
            htmlBody: "<p>Your ticket</p>",
            attendeeFilter: new BulkEmailAttendeeFilter(),
            triggeredBy: EmailAddress.From("admin@example.com"),
            now: DateTimeOffset.UtcNow);

        await Environment.EmailDatabase.SeedAsync(db => db.BulkEmailJobs.Add(job));

        var sut = new GetBulkEmailsHandler(Environment.EmailDatabase.Context);

        // Act: query with team B's ID
        var result = await sut.HandleAsync(
            new GetBulkEmailsQuery(eventId, teamIdB),
            testContext.CancellationToken);

        // Assert: cross-team access returns empty list
        result.ShouldBeEmpty();
    }
}
