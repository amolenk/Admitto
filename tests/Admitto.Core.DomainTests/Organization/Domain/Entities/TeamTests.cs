using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Testing.Builders.Organization.Domain;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

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
        var newName = DisplayName.From("Updated Name");

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
        var result = ErrorResult.Capture(() => sut.ChangeName(DisplayName.From("New Name")));

        // Assert
        result.Error.ShouldMatch(Team.Errors.TeamArchived(sut.Id));
    }

    // -------------------------------------------------------------------------
    // ChangeEmailAddress()
    // -------------------------------------------------------------------------

    [TestMethod]
    public void ChangeEmailAddress_ActiveTeam_UpdatesEmail()
    {
        // Arrange
        var sut = new TeamBuilder().Build();
        var newEmail = EmailAddress.From("new@example.com");

        // Act
        sut.ChangeEmailAddress(newEmail);

        // Assert
        sut.EmailAddress.ShouldBe(newEmail);
    }

    [TestMethod]
    public void ChangeEmailAddress_ArchivedTeam_ThrowsTeamArchived()
    {
        // Arrange
        var sut = new TeamBuilder().AsArchived().Build();

        // Act
        var result = ErrorResult.Capture(() => sut.ChangeEmailAddress(EmailAddress.From("new@example.com")));

        // Assert
        result.Error.ShouldMatch(Team.Errors.TeamArchived(sut.Id));
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
