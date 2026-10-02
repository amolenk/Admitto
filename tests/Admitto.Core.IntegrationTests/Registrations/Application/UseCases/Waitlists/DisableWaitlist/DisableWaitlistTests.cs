using Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.DisableWaitlist;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Waitlists.DisableWaitlist;

[TestClass]
public sealed class DisableWaitlistTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given a waitlisted registration whose single active waitlist entry is its only selection
    // When an organizer explicitly disables that ticket type's waitlist
    // Then the registration is cancelled with the ticket-types-removed reason (no email)
    [TestMethod]
    public async ValueTask DisableWaitlist_LastSelectionForRegistration_CancelsRegistration()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var ticketTypeId = TicketTypeId.New();
        var email = EmailAddress.From("alice@example.com");
        RegistrationId registrationId = RegistrationId.New();

        await Environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var catalog = TicketCatalog.Create(eventId, teamId);
            catalog.AddTicketType(ticketTypeId, TicketTypeName.From("Workshop"), [], publicCapacity: 1, waitlistEnabled: true);
            catalog.Claim([ticketTypeId], ClaimMode.Public);
            catalog.ClearDomainEvents();
            dbContext.TicketCatalogs.Add(catalog);

            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                eventId,
                teamId,
                EventName.From("Conference"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31),
                TimeZoneId.From("UTC"));
            ticketedEvent.ClearDomainEvents();
            dbContext.TicketedEvents.Add(ticketedEvent);

            var registration = Registration.Create(
                teamId, eventId, email, FirstName.From("Alice"), LastName.From("Doe"), []);
            registration.ClearDomainEvents();
            registrationId = registration.Id;
            dbContext.Registrations.Add(registration);

            var waitlist = Waitlist.Create(eventId, ticketTypeId, teamId);
            waitlist.AddEntry(email, DateTimeOffset.UtcNow, catalog, registrationId);
            waitlist.ClearDomainEvents();
            dbContext.Waitlists.Add(waitlist);
        });

        var sut = new DisableWaitlistHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);
        await sut.HandleAsync(
            new DisableWaitlistCommand(eventId.Value, teamId.Value, ticketTypeId.Value, FreedSlots: 0),
            testContext.CancellationToken);
        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleAsync(
                r => r.Id == registrationId, testContext.CancellationToken);
            registration.Status.ShouldBe(RegistrationStatus.Cancelled);
            registration.CancellationReason.ShouldBe(CancellationReason.TicketTypesRemoved);
        });
    }

    // Given a waitlisted registration queued on two ticket types
    // When an organizer disables the waitlist for only one of them
    // Then the registration is not cancelled, since it still has a selection on the other
    [TestMethod]
    public async ValueTask DisableWaitlist_AnotherSelectionRemains_DoesNotCancelRegistration()
    {
        var teamId = TeamId.New();
        var eventId = TicketedEventId.New();
        var ticketTypeId = TicketTypeId.New();
        var otherTicketTypeId = TicketTypeId.New();
        var email = EmailAddress.From("alice@example.com");
        RegistrationId registrationId = RegistrationId.New();

        await Environment.RegistrationsDatabase.SeedAsync(dbContext =>
        {
            var catalog = TicketCatalog.Create(eventId, teamId);
            catalog.AddTicketType(ticketTypeId, TicketTypeName.From("Workshop"), [], publicCapacity: 1, waitlistEnabled: true);
            catalog.AddTicketType(otherTicketTypeId, TicketTypeName.From("Masterclass"), [], publicCapacity: 1, waitlistEnabled: true);
            catalog.Claim([ticketTypeId], ClaimMode.Public);
            catalog.Claim([otherTicketTypeId], ClaimMode.Public);
            catalog.ClearDomainEvents();
            dbContext.TicketCatalogs.Add(catalog);

            var ticketedEvent = TicketedEvent.Create(
                CreationRequestId.From(Guid.NewGuid()),
                eventId,
                teamId,
                EventName.From("Conference"),
                AbsoluteUrl.From("https://example.com"),
                AbsoluteUrl.From("https://tickets.example.com"),
                DateTimeOffset.UtcNow.AddDays(30),
                DateTimeOffset.UtcNow.AddDays(31),
                TimeZoneId.From("UTC"));
            ticketedEvent.ClearDomainEvents();
            dbContext.TicketedEvents.Add(ticketedEvent);

            var registration = Registration.Create(
                teamId, eventId, email, FirstName.From("Alice"), LastName.From("Doe"), []);
            registration.ClearDomainEvents();
            registrationId = registration.Id;
            dbContext.Registrations.Add(registration);

            var waitlist = Waitlist.Create(eventId, ticketTypeId, teamId);
            waitlist.AddEntry(email, DateTimeOffset.UtcNow, catalog, registrationId);
            waitlist.ClearDomainEvents();
            dbContext.Waitlists.Add(waitlist);

            var otherWaitlist = Waitlist.Create(eventId, otherTicketTypeId, teamId);
            otherWaitlist.AddEntry(email, DateTimeOffset.UtcNow, catalog, registrationId);
            otherWaitlist.ClearDomainEvents();
            dbContext.Waitlists.Add(otherWaitlist);
        });

        var sut = new DisableWaitlistHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);
        await sut.HandleAsync(
            new DisableWaitlistCommand(eventId.Value, teamId.Value, ticketTypeId.Value, FreedSlots: 0),
            testContext.CancellationToken);
        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations.SingleAsync(
                r => r.Id == registrationId, testContext.CancellationToken);
            registration.Status.ShouldBe(RegistrationStatus.Waitlisted);
            registration.CancellationReason.ShouldBeNull();
        });
    }
}
