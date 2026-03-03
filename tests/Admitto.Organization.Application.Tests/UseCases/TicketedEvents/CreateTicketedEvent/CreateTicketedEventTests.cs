using Amolenk.Admitto.Organization.Application.Tests.Infrastructure;
using Amolenk.Admitto.Organization.Application.UseCases.TicketedEvents.CreateTicketedEvent;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Should = Shouldly.Should;

namespace Amolenk.Admitto.Organization.Application.Tests.UseCases.TicketedEvents.CreateTicketedEvent;

[TestClass]
public sealed class CreateTicketedEventTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask CreateTicketedEvent_ValidCommand_CreatesTicketedEvent()
    {
        // Arrange
        var command = NewCreateTicketedEventCommand();
        var sut = NewCreateTicketedEventHandler();

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        // Assert
        await Environment.Database.AssertAsync(async dbContext =>
        {
            var ticketedEvent = await dbContext.TicketedEvents.SingleOrDefaultAsync(testContext.CancellationToken);

            ticketedEvent.ShouldNotBeNull();
            ticketedEvent.TeamId.Value.ShouldBe(command.TeamId);
            ticketedEvent.Slug.Value.ShouldBe(command.Slug);
            ticketedEvent.Name.Value.ShouldBe(command.Name);
            ticketedEvent.WebsiteUrl.Value.ToString().ShouldBe(command.WebsiteUrl);
            ticketedEvent.BaseUrl.Value.ToString().ShouldBe(command.BaseUrl);
            ticketedEvent.EventWindow.Start.ShouldBe(command.StartsAt);
            ticketedEvent.EventWindow.End.ShouldBe(command.EndsAt);
        });
    }

    [TestMethod]
    public async ValueTask CreateTicketedEvent_DuplicateSlugWithinSameTeam_ThrowsDbUpdateException()
    {
        // Arrange
        var fixture = CreateTicketedEventFixture.DuplicateSlugWithinSameTeam();
        await fixture.SetupAsync(Environment);

        var command = NewCreateTicketedEventCommand(
            teamId: fixture.TeamIdValue,
            slug: fixture.TicketedEventSlug,
            name: "Another Event",
            websiteUrl: "https://example.com/events/another-event");
        var sut = NewCreateTicketedEventHandler();

        // Act
        await sut.HandleAsync(command, testContext.CancellationToken);

        var exception = Should.Throw<DbUpdateException>(
            () => Environment.Database.Context.SaveChangesAsync(testContext.CancellationToken));

        // Assert
        exception.InnerException
            .ShouldBeAssignableTo<PostgresException>()?
            .ConstraintName.ShouldBe("IX_ticketed_events_team_id_slug");
    }

    private static CreateTicketedEventCommand NewCreateTicketedEventCommand(
        Guid? teamId = null,
        string? slug = null,
        string? name = null,
        string? websiteUrl = null,
        string? baseUrl = null,
        DateTimeOffset? startsAt = null,
        DateTimeOffset? endsAt = null)
    {
        teamId ??= Guid.NewGuid();
        slug ??= "build-stuff";
        name ??= "Build Stuff";
        websiteUrl ??= "https://example.com/events/build-stuff";
        baseUrl ??= "https://tickets.example.com";
        startsAt ??= new DateTimeOffset(2026, 4, 1, 9, 0, 0, TimeSpan.Zero);
        endsAt ??= new DateTimeOffset(2026, 4, 3, 17, 0, 0, TimeSpan.Zero);

        return new CreateTicketedEventCommand(
            teamId.Value,
            slug,
            name,
            websiteUrl,
            baseUrl,
            startsAt.Value,
            endsAt.Value);
    }

    private static CreateTicketedEventHandler NewCreateTicketedEventHandler() =>
        new(Environment.Database.Context);
}
