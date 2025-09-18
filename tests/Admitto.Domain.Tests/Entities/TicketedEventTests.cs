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

    private static TicketType AddTicketType(TicketedEvent ticketedEvent, Action<TicketTypeBuilder>? configure = null)
    {
        var builder = new TicketTypeBuilder();
        configure?.Invoke(builder);

        // Build the ticket type to get the parameter values
        var ticketType = builder.Build();

        // Add the ticket type to the event
        ticketedEvent.AddTicketType(ticketType.Slug, ticketType.Name, ticketType.SlotName, ticketType.MaxCapacity);

        // Return the added ticket type from the event's collection
        return ticketedEvent.TicketTypes.First(tt => tt.Slug == ticketType.Slug);
    }
}