// using Amolenk.Admitto.Domain.Entities;
// using Amolenk.Admitto.Domain.Exceptions;
//
// namespace Amolenk.Admitto.Domain.Tests.Entities;
//
// public class TicketedEventTests
// {
//     private static readonly Guid AttendeeId = Guid.NewGuid();
//     
//     [Test]
//     public async Task ClaimTickets_TicketsAvailable_ReducesAvailability()
//     {
//         // Arrange
//         const int maxCapacity = 1;
//         var ticketedEvent = CreateTicketedEventAggregate(maxCapacity);
//         
//         // Act
//         ticketedEvent.ClaimTickets(AttendeeId);
//         
//         // Assert
//         await Assert.That(ticketedEvent.AvailableCapacity).IsEqualTo(maxCapacity - 1);
//     }
//
//     [Test]
//     public async Task ClaimTickets_TicketsAvailable_AddsClaimWithExpiration()
//     {
//         // Arrange
//         var ticketedEvent = CreateTicketedEventAggregate();
//         
//         // Act
//         var ticketClaim = ticketedEvent.ClaimTickets(AttendeeId);
//
//         // Assert
//         await Assert.That(ticketedEvent.TicketClaims).HasSingleItem().And.Contains(ticketClaim);
//
//         // Verify that expiration is approximately 15 minutes in the future.
//         var expirationLowerBound = DateTime.Now.AddMinutes(15).AddSeconds(-5);
//         var expirationUpperBound = expirationLowerBound.AddSeconds(10);
//         //
//         await Assert.That(ticketClaim.ExpirationTime)
//             .IsGreaterThanOrEqualTo(expirationLowerBound)
//             .And
//             .IsLessThanOrEqualTo(expirationUpperBound);
//     }
//
//     [Test]
//     public async Task ClaimTickets_DuplicateClaim_ResetsExpiration()
//     {
//         // Arrange
//         
//         // Set max capacity to 1 to verify that we don't get a TicketsUnavailableException.
//         var ticketedEvent = CreateTicketedEventAggregate(maxCapacity: 1);
//         
//         // Act
//         var ticketClaim = ticketedEvent.ClaimTickets(AttendeeId);
//         var originalExpirationTime = ticketClaim.ExpirationTime;
//         var originalAvailability = ticketedEvent.AvailableCapacity;
//
//         ticketClaim = ticketedEvent.ClaimTickets(AttendeeId);
//
//         // Assert
//         await Assert.That(ticketedEvent.TicketClaims).HasSingleItem().And.Contains(ticketClaim);
//
//         // Verify that expiration is reset.
//         await Assert.That(ticketClaim.ExpirationTime).IsGreaterThan(originalExpirationTime);
//         
//         // Available capacity should stay the same.
//         await Assert.That(ticketedEvent.AvailableCapacity).IsEqualTo(originalAvailability);
//     }
//
//     [Test]
//     public void ClaimTickets_TicketsUnavailable_ThrowsException()
//     {
//         // Arrange
//         var ticketedEvent = CreateTicketedEventAggregate(maxCapacity: 0);
//         
//         // Act & Assert
//         Assert.Throws<TicketsUnavailableException>(() => ticketedEvent.ClaimTickets(AttendeeId));
//     }
//
//     private static TicketedEvent CreateTicketedEventAggregate(int maxCapacity = 100)
//     {
//         return TicketedEvent.Create("Ticketed Event", DateTime.Today.AddMonths(1), maxCapacity);
//     }
// }