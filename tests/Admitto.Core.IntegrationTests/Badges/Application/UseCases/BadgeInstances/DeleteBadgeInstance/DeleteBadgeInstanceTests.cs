using Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeInstances.DeleteBadgeInstance;
using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Should = Shouldly.Should;
using CoreBadgeInstanceId = Amolenk.Admitto.Core.Badges.Domain.ValueObjects.BadgeInstanceId;

namespace Amolenk.Admitto.Core.IntegrationTests.Badges.Application.UseCases.BadgeInstances.DeleteBadgeInstance;

[TestClass]
public sealed class DeleteBadgeInstanceTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an existing badge instance on an active event
    // When it is deleted
    // Then it is removed from the database
    [TestMethod]
    public async ValueTask DeleteBadgeInstance_ExistingInstance_RemovesFromDatabase()
    {
        var fixture = DeleteBadgeInstanceFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new DeleteBadgeInstanceCommand(
            fixture.EventId, fixture.TeamId, fixture.BadgeTypeId, fixture.BadgeInstanceId);

        var sut = new DeleteBadgeInstanceHandler(Environment.BadgesDatabase.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.BadgesDatabase.AssertAsync(async db =>
        {
            var instance = await db.BadgeInstances.FindAsync(
                [CoreBadgeInstanceId.From(fixture.BadgeInstanceId)], testContext.CancellationToken);

            instance.ShouldBeNull();
        });
    }

    // Given a badge event that has been archived
    // When a badge instance belonging to it is deleted
    // Then it throws a business rule violation because the event is no longer active
    [TestMethod]
    public async ValueTask DeleteBadgeInstance_ArchivedEvent_ThrowsEventNotActiveError()
    {
        var fixture = DeleteBadgeInstanceFixture.ArchivedEvent();
        await fixture.SetupAsync(Environment);

        var command = new DeleteBadgeInstanceCommand(
            fixture.EventId, fixture.TeamId, fixture.BadgeTypeId, fixture.BadgeInstanceId);

        var sut = new DeleteBadgeInstanceHandler(Environment.BadgesDatabase.Context);

        var exception = await Should.ThrowAsync<BusinessRuleViolationException>(
            async () => await sut.HandleAsync(command, testContext.CancellationToken));

        exception.Error.ShouldMatch(BadgeEvent.Errors.EventNotActive);
    }
}
