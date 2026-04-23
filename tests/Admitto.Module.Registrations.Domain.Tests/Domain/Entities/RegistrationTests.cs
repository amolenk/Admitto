using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Module.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class RegistrationTests
{
    private static readonly TeamId DefaultTeamId = TeamId.New();
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly EmailAddress DefaultEmail = EmailAddress.From("test@example.com");

    [TestMethod]
    public void SC001_Registration_Create_ValidInput_CreatesWithCorrectSnapshots()
    {
        // Arrange
        var slug1 = "general-admission";
        var slug2 = "vip-pass";
        var timeSlots1 = new[] { "morning", "afternoon" };
        var timeSlots2 = new[] { "evening" };

        var tickets = new List<TicketTypeSnapshot>
        {
            new(slug1, timeSlots1),
            new(slug2, timeSlots2)
        };

        // Act
        var sut = Registration.Create(DefaultTeamId, DefaultEventId, DefaultEmail, tickets);

        // Assert
        sut.Tickets.Count.ShouldBe(2);
        sut.Tickets.ShouldContain(t => t.Slug == slug1 && t.TimeSlots.SequenceEqual(timeSlots1));
        sut.Tickets.ShouldContain(t => t.Slug == slug2 && t.TimeSlots.SequenceEqual(timeSlots2));
    }

    [TestMethod]
    public void SC002_Registration_Create_DuplicateSlugs_ThrowsBusinessRuleViolation()
    {
        // Arrange
        var duplicateSlug = "general-admission";

        var tickets = new List<TicketTypeSnapshot>
        {
            new(duplicateSlug, []),
            new(duplicateSlug, [])
        };

        // Act
        var result = ErrorResult.Capture(() =>
            Registration.Create(DefaultTeamId, DefaultEventId, DefaultEmail, tickets));

        // Assert
        result.Error.ShouldMatch(Registration.Errors.DuplicateTicketTypes([duplicateSlug]));
    }
}