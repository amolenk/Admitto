using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.Tests.TestHelpers.Builders;
using Amolenk.Admitto.Domain.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Domain.Tests.Entities;

[TestClass]
public class TicketedEventTests
{
    [TestMethod]
    public void ClaimTickets_HasAvailability_ReducesAvailability()
    {
        // Arrange
        const int maxCapacity = 1;
        var ticketedEvent = new TicketedEventBuilder().Build();
        var ticketType = AddTicketType(ticketedEvent, builder => builder.WithMaxCapacity(maxCapacity));

        const string email = "alice@example.com";
        var registrationDateTime = DateTime.UtcNow;
        var ticketSelection = new TicketSelection(ticketType.Slug, 1);

        // Act
        ticketedEvent.ClaimTickets(email, registrationDateTime, [ticketSelection]);

        // Assert
        ticketType.UsedCapacity.ShouldBe(1);
        ticketType.HasAvailableCapacity().ShouldBe(false);
    }

    [TestMethod]
    public void ClaimTickets_TicketsUnavailable_ThrowsException()
    {
        // Arrange
        var ticketedEvent = new TicketedEventBuilder().Build();
        var ticketType = AddTicketType(ticketedEvent, builder => builder.WithMaxCapacity(0));

        const string email = "alice@example.com";
        var registrationDateTime = DateTime.UtcNow;
        var ticketSelection = new TicketSelection(ticketType.Slug, 1);

        // Act & Assert
        // TODO Check error code
        Should.Throw<DomainRuleException>(() => ticketedEvent.ClaimTickets(
            email,
            registrationDateTime,
            [ticketSelection]));
    }

    [TestMethod]
    public void ClaimTickets_NoSlotOverlap_Succeeds()
    {
        // Arrange
        var ticketedEvent = new TicketedEventBuilder().Build();
        var ticketType1 = AddTicketType(ticketedEvent, builder => 
            builder.WithSlug("morning").WithName("Morning Session").WithSlotName("morning"));
        var ticketType2 = AddTicketType(ticketedEvent, builder => 
            builder.WithSlug("afternoon").WithName("Afternoon Session").WithSlotName("afternoon"));

        const string email = "alice@example.com";
        var registrationDateTime = DateTime.UtcNow;
        var ticketSelections = new List<TicketSelection>
        {
            new(ticketType1.Slug, 1),
            new(ticketType2.Slug, 1)
        };

        // Act
        ticketedEvent.ClaimTickets(email, registrationDateTime, ticketSelections);

        // Assert
        ticketType1.UsedCapacity.ShouldBe(1);
        ticketType2.UsedCapacity.ShouldBe(1);
    }

    [TestMethod]
    public void ClaimTickets_SameSlotMultipleTimes_ThrowsOverlapException()
    {
        // Arrange
        var ticketedEvent = new TicketedEventBuilder().Build();
        var ticketType1 = AddTicketType(ticketedEvent, builder => 
            builder.WithSlug("morning1").WithName("Morning Workshop A").WithSlotName("morning"));
        var ticketType2 = AddTicketType(ticketedEvent, builder => 
            builder.WithSlug("morning2").WithName("Morning Workshop B").WithSlotName("morning"));

        const string email = "alice@example.com";
        var registrationDateTime = DateTime.UtcNow;
        var ticketSelections = new List<TicketSelection>
        {
            new(ticketType1.Slug, 1),
            new(ticketType2.Slug, 1)
        };

        // Act & Assert
        var exception = Should.Throw<DomainRuleException>(() => ticketedEvent.ClaimTickets(
            email,
            registrationDateTime,
            ticketSelections));
        
        exception.ErrorCode.ShouldBe("ticketed_event.overlapping_slots");
        exception.Message.ShouldContain("morning");
    }

    [TestMethod]
    public void ClaimTickets_MultipleQuantitySameTicket_ThrowsOverlapException()
    {
        // Arrange
        var ticketedEvent = new TicketedEventBuilder().Build();
        var ticketType = AddTicketType(ticketedEvent, builder => 
            builder.WithSlug("workshop").WithName("Workshop").WithSlotName("morning").WithMaxCapacity(5));

        const string email = "alice@example.com";
        var registrationDateTime = DateTime.UtcNow;
        var ticketSelection = new TicketSelection(ticketType.Slug, 2); // Trying to claim 2 tickets

        // Act & Assert
        var exception = Should.Throw<DomainRuleException>(() => ticketedEvent.ClaimTickets(
            email,
            registrationDateTime,
            [ticketSelection]));
        
        exception.ErrorCode.ShouldBe("ticketed_event.overlapping_slots");
        exception.Message.ShouldContain("morning");
    }
    [TestMethod]
    public void ClaimTickets_MultiSlotTicketType_WithOverlap_ThrowsOverlapException()
    {
        // Arrange
        var ticketedEvent = new TicketedEventBuilder().Build();
        var fullDayTicket = AddTicketType(ticketedEvent, builder => 
            builder.WithSlug("full-day").WithName("Full Day Workshop")
                   .WithSlotNames(new List<string> { "morning", "afternoon" }));
        var morningTicket = AddTicketType(ticketedEvent, builder => 
            builder.WithSlug("morning").WithName("Morning Session").WithSlotName("morning"));

        const string email = "alice@example.com";
        var registrationDateTime = DateTime.UtcNow;
        var ticketSelections = new List<TicketSelection>
        {
            new(fullDayTicket.Slug, 1),
            new(morningTicket.Slug, 1)
        };

        // Act & Assert
        var exception = Should.Throw<DomainRuleException>(() => ticketedEvent.ClaimTickets(
            email,
            registrationDateTime,
            ticketSelections));
        
        exception.ErrorCode.ShouldBe("ticketed_event.overlapping_slots");
        exception.Message.ShouldContain("morning");
    }

    [TestMethod]
    public void ClaimTickets_MultiSlotTicketType_NoOverlap_Succeeds()
    {
        // Arrange
        var ticketedEvent = new TicketedEventBuilder().Build();
        var fullDayTicket = AddTicketType(ticketedEvent, builder => 
            builder.WithSlug("full-day").WithName("Full Day Workshop")
                   .WithSlotNames(new List<string> { "morning", "afternoon" }));
        var eveningTicket = AddTicketType(ticketedEvent, builder => 
            builder.WithSlug("evening").WithName("Evening Session").WithSlotName("evening"));

        const string email = "alice@example.com";
        var registrationDateTime = DateTime.UtcNow;
        var ticketSelections = new List<TicketSelection>
        {
            new(fullDayTicket.Slug, 1),
            new(eveningTicket.Slug, 1)
        };

        // Act
        ticketedEvent.ClaimTickets(email, registrationDateTime, ticketSelections);

        // Assert
        fullDayTicket.UsedCapacity.ShouldBe(1);
        eveningTicket.UsedCapacity.ShouldBe(1);
    }

    private static TicketType AddTicketType(TicketedEvent ticketedEvent, Action<TicketTypeBuilder>? configure = null)
    {
        var builder = new TicketTypeBuilder();
        configure?.Invoke(builder);

        // Build the ticket type to get the parameter values
        var ticketType = builder.Build();

        // Add the ticket type to the event using the appropriate method
        if (ticketType.SlotNames.Count == 1)
        {
            ticketedEvent.AddTicketType(ticketType.Slug, ticketType.Name, ticketType.SlotNames[0], ticketType.MaxCapacity);
        }
        else
        {
            ticketedEvent.AddTicketType(ticketType.Slug, ticketType.Name, ticketType.SlotNames, ticketType.MaxCapacity);
        }

        // Return the added ticket type from the event's collection
        return ticketedEvent.TicketTypes.First(tt => tt.Slug == ticketType.Slug);
    }
}