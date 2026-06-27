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

    [TestMethod]
    public void Create_NoAccentColor_UsesDefaultAccentColor()
    {
        var sut = Team.Create(TeamName.From("New Team"));

        sut.AccentColor.ShouldBe(TeamAccentColor.From(TeamAccentColor.Default));
    }

    [TestMethod]
    public void ChangeAccentColor_ActiveTeam_UpdatesAccentColor()
    {
        var sut = new TeamBuilder().Build();

        sut.ChangeAccentColor(TeamAccentColor.From("#0f766e"));

        sut.AccentColor.Value.ShouldBe("#0f766e");
    }

    [TestMethod]
    public void ChangeAccentColor_ArchivedTeam_ThrowsTeamArchived()
    {
        var sut = new TeamBuilder().AsArchived().Build();

        var result = ErrorResult.Capture(() => sut.ChangeAccentColor(TeamAccentColor.From("#0f766e")));

        result.Error.ShouldMatch(Team.Errors.TeamArchived(sut.Id));
    }

    [TestMethod]
    public void TeamAccentColor_InvalidFormat_Throws()
    {
        void Act() => TeamAccentColor.From("not-a-color");

        Should.Throw<ValueObjectValidationException>(Act);
    }

    // -------------------------------------------------------------------------
    // Archive()
    // -------------------------------------------------------------------------

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
