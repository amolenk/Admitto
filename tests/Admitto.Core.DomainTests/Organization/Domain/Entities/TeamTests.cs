using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Testing.Builders.Organization.Domain;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;
using Vogen;

namespace Amolenk.Admitto.Core.Organization.Domain.Tests.Entities;

[TestClass]
public sealed class TeamTests
{
    // -------------------------------------------------------------------------
    // ChangeName()
    // -------------------------------------------------------------------------

    // Given an active team
    // When the name is changed
    // Then the team's name is updated
    [TestMethod]
    public void ChangeName_ActiveTeam_UpdatesName()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var newName = TeamName.From("Updated Name");

        // Act
        sut.ChangeName(newName);

        // Assert
        sut.Name.ShouldBe(newName);
    }

    // Given an archived team
    // When the name is changed
    // Then it throws TeamArchived
    [TestMethod]
    public void ChangeName_ArchivedTeam_ThrowsTeamArchived()
    {
        // Arrange
        var sut = new TeamBuilder().AsArchived().Build();

        // Act
        var result = ErrorResult.Capture(() => sut.ChangeName(TeamName.From("New Name")));

        // Assert
        result.Error.ShouldMatch(Team.Errors.TeamArchived(sut.Id));
    }

    // When a team is created without specifying an accent color
    // Then it uses the default accent color
    [TestMethod]
    public void Create_NoAccentColor_UsesDefaultAccentColor()
    {
        var sut = Team.Create(TeamName.From("New Team"));

        sut.AccentColor.ShouldBe(AccentColor.From(AccentColor.Default));
    }

    // Given an active team
    // When the accent color is changed
    // Then the team's accent color is updated
    [TestMethod]
    public void ChangeAccentColor_ActiveTeam_UpdatesAccentColor()
    {
        var sut = new TeamBuilder().Build();

        sut.ChangeAccentColor(AccentColor.From("#0f766e"));

        sut.AccentColor.Value.ShouldBe("#0f766e");
    }

    // Given an archived team
    // When the accent color is changed
    // Then it throws TeamArchived
    [TestMethod]
    public void ChangeAccentColor_ArchivedTeam_ThrowsTeamArchived()
    {
        var sut = new TeamBuilder().AsArchived().Build();

        var result = ErrorResult.Capture(() => sut.ChangeAccentColor(AccentColor.From("#0f766e")));

        result.Error.ShouldMatch(Team.Errors.TeamArchived(sut.Id));
    }

    // When an accent color is created from an invalid format string
    // Then it throws a value object validation exception
    [TestMethod]
    public void AccentColor_InvalidFormat_Throws()
    {
        void Act() => AccentColor.From("not-a-color");

        Should.Throw<ValueObjectValidationException>(Act);
    }

    // -------------------------------------------------------------------------
    // Archive()
    // -------------------------------------------------------------------------

    // Given an active team
    // When it is archived
    // Then it is marked archived with the given archived-at timestamp
    [TestMethod]
    public void Archive_ActiveTeam_SetsArchivedAt()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var now = DateTimeOffset.UtcNow;

        // Act
        sut.Archive(now);

        // Assert
        sut.IsArchived.ShouldBeTrue();
        sut.ArchivedAt.ShouldBe(now);
    }

    // Given an already archived team
    // When it is archived again
    // Then it throws TeamAlreadyArchived
    [TestMethod]
    public void Archive_AlreadyArchivedTeam_ThrowsAlreadyArchived()
    {
        // Arrange
        var sut = new TeamBuilder().AsArchived().Build();

        // Act
        var result = ErrorResult.Capture(() => sut.Archive(DateTimeOffset.UtcNow));

        // Assert
        result.Error.ShouldMatch(Team.Errors.TeamAlreadyArchived(sut.Id));
    }
}
