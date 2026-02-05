using Amolenk.Admitto.Registrations.Application.Tests.Builders;
using Amolenk.Admitto.Registrations.Domain.Entities;
using Amolenk.Admitto.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class RegistrationTests
{
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly EmailAddress DefaultEmail = EmailAddress.From("test@example.com");
    private static readonly TimeSlot DefaultTimeSlot = TimeSlot.From("default");

    [TestMethod]
    [DataRow(TicketGrantMode.Privileged)]
    [DataRow(TicketGrantMode.SelfService)]
    public void GrantTickets_SingleTicket_GrantsTicket(TicketGrantMode grantMode)
    {
        // Arrange
        var sut = new RegistrationBuilder().Build();

        var ticketType = NewTicketTypeSnapshot();
        var ticketRequest = NewTicketRequest(ticketType, grantMode);

        // Act
        sut.GrantTickets([ticketRequest], [ticketType]);

        // Asserts
        sut.Tickets.Count.ShouldBe(1);
        sut.Tickets.ShouldContain(t => t.TicketTypeId == ticketType.Id);
    }

    [TestMethod]
    [DataRow(TicketGrantMode.Privileged)]
    [DataRow(TicketGrantMode.SelfService)]
    public void GrantTickets_MultipleTickets_GrantsTickets(TicketGrantMode grantMode)
    {
        // Arrange
        var sut = NewRegistration();

        var conferenceTimeSlot = TimeSlot.From("conference");
        var workshopTimeSlot = TimeSlot.From("workshop");
        var conferenceTicketType = NewTicketTypeSnapshot(timeSlots: [conferenceTimeSlot]);
        var workshopTicketType = NewTicketTypeSnapshot(timeSlots: [workshopTimeSlot]);
        var conferenceTicketRequest = NewTicketRequest(conferenceTicketType, grantMode);
        var workshopTicketRequest = NewTicketRequest(workshopTicketType, grantMode);
        
        // Act
        sut.GrantTickets(
            [conferenceTicketRequest, workshopTicketRequest], 
            [conferenceTicketType, workshopTicketType]);
    
        // Assert
        sut.Tickets.ShouldContain(t => t.TicketTypeId == conferenceTicketType.Id);
        sut.Tickets.ShouldContain(t => t.TicketTypeId == workshopTicketType.Id);
    }
    
    [TestMethod]
    [DataRow(TicketGrantMode.Privileged)]
    [DataRow(TicketGrantMode.SelfService)]
    public void GrantTickets_TicketTypesUnknown_ReturnsUnknownTicketTypesError(TicketGrantMode grantMode)
    {
        // Arrange
        var sut = NewRegistration();
    
        var existingTicketType = NewTicketTypeSnapshot();
        var unknownTicketType1 = NewTicketTypeSnapshot();
        var unknownTicketType2 = NewTicketTypeSnapshot();
        var existingTicketRequest = NewTicketRequest(existingTicketType, grantMode);
        var unknownTicketRequest1 = NewTicketRequest(unknownTicketType1, grantMode);
        var unknownTicketRequest2 = NewTicketRequest(unknownTicketType2, grantMode);
    
        // Act
        var result = ErrorResult.Capture(() => sut.GrantTickets(
            [existingTicketRequest, unknownTicketRequest1, unknownTicketRequest2],
            [existingTicketType]));
    
        // Assert
        result.Error.ShouldMatch(
            Registration.Errors.UnknownTicketTypes([unknownTicketType1.Id, unknownTicketType2.Id]));
    
        sut.Tickets.ShouldBeEmpty();
    }
    
    #region Test scenarios to be implemented
    
    // [TestMethod]
    // [DataRow(TicketGrantMode.Privileged)]
    // [DataRow(TicketGrantMode.SelfService)]
    // public void GrantTickets_DuplicateTicketTypeIds_ReturnsDuplicateTicketTypesError(TicketGrantMode grantMode)
    // {
    //     // Given
    //     var sut = NewRegistration();
    //
    //     var ticketTypeId = TicketTypeId.New();
    //     var ticketTypes = new Dictionary<TicketTypeId, TicketTypeSnapshot>
    //     {
    //         [ticketTypeId] = NewTicketTypeSnapshot(ticketTypeId)
    //     };
    //
    //     // When
    //     var result = sut.GrantTickets([ticketTypeId, ticketTypeId], ticketTypes, grantMode);
    //
    //     // Then
    //     result.IsSuccess.ShouldBeFalse();
    //     result.Error.ShouldMatch(
    //         RegistrationErrors.DuplicateTicketTypes([ticketTypeId]));
    //
    //     sut.Tickets.ShouldBeEmpty();
    // }
    //
    // [TestMethod]
    // [DataRow(TicketGrantMode.Privileged)]
    // [DataRow(TicketGrantMode.SelfService)]
    // public void GrantTickets_TicketTypeAlreadyGranted_ReturnsTicketTypeAlreadyGrantedError(TicketGrantMode grantMode)
    // {
    //     // Given
    //     var sut = NewRegistration();
    //
    //     var ticketTypeId = TicketTypeId.New();
    //     var ticketTypes = new Dictionary<TicketTypeId, TicketTypeSnapshot>
    //     {
    //         [ticketTypeId] = NewTicketTypeSnapshot(ticketTypeId)
    //     };
    //
    //     sut.GrantTickets([ticketTypeId], ticketTypes, TicketGrantMode.Privileged);
    //
    //     // When
    //     var result = sut.GrantTickets([ticketTypeId], ticketTypes, grantMode);
    //
    //     // Then
    //     result.IsSuccess.ShouldBeFalse();
    //     result.Error.ShouldMatch(RegistrationErrors.TicketTypeAlreadyGranted(ticketTypeId));
    //
    //     sut.Tickets.Count.ShouldBe(1);
    // }
    //
    // [TestMethod]
    // [DataRow(TicketGrantMode.Privileged)]
    // [DataRow(TicketGrantMode.SelfService)]
    // public void GrantTickets_OverlappingTimeSlotsInNewTickets_ReturnsOverlappingTicketsError(TicketGrantMode grantMode)
    // {
    //     // Given
    //     var sut = NewRegistration();
    //
    //     var morningTimeSlot = TimeSlot.From("morning");
    //     var morningTicketTypeId = TicketTypeId.New();
    //     var alsoMorningTicketTypeId = TicketTypeId.New();
    //
    //     var ticketTypes = new Dictionary<TicketTypeId, TicketTypeSnapshot>
    //     {
    //         [morningTicketTypeId] = NewTicketTypeSnapshot(morningTicketTypeId, [morningTimeSlot]),
    //         [alsoMorningTicketTypeId] = NewTicketTypeSnapshot(alsoMorningTicketTypeId, [morningTimeSlot]),
    //     };
    //
    //     // When
    //     var result = sut.GrantTickets(
    //         [morningTicketTypeId, alsoMorningTicketTypeId],
    //         ticketTypes,
    //         grantMode);
    //
    //     // Then
    //     result.IsSuccess.ShouldBeFalse();
    //     result.Error.ShouldMatch(
    //         RegistrationErrors.OverlappingTicketTypeTimeSlots(
    //             [morningTicketTypeId, alsoMorningTicketTypeId]));
    //
    //     sut.Tickets.ShouldBeEmpty();
    // }
    //
    // [TestMethod]
    // [DataRow(TicketGrantMode.Privileged)]
    // [DataRow(TicketGrantMode.SelfService)]
    // public void GrantTickets_OverlappingTimeSlotsWithCurrentTickets_ReturnsOverlappingTicketsError(TicketGrantMode grantMode)
    // {
    //     // Given
    //     var sut = NewRegistration();
    //
    //     var morningTimeSlot = TimeSlot.From("morning");
    //     var morningTicketTypeId = TicketTypeId.New();
    //     var alsoMorningTicketTypeId = TicketTypeId.New();
    //
    //     var ticketTypes = new Dictionary<TicketTypeId, TicketTypeSnapshot>
    //     {
    //         [morningTicketTypeId] = NewTicketTypeSnapshot(morningTicketTypeId, [morningTimeSlot]),
    //         [alsoMorningTicketTypeId] = NewTicketTypeSnapshot(alsoMorningTicketTypeId, [morningTimeSlot]),
    //     };
    //
    //     sut.GrantTickets([morningTicketTypeId], ticketTypes, TicketGrantMode.Privileged);
    //     
    //     // When
    //     var result = sut.GrantTickets([alsoMorningTicketTypeId], ticketTypes, grantMode);
    //
    //     // Then
    //     result.IsSuccess.ShouldBeFalse();
    //     result.Error.ShouldMatch(
    //         RegistrationErrors.OverlappingTicketTypeTimeSlots([morningTicketTypeId, alsoMorningTicketTypeId]));
    //
    //     sut.Tickets.Count.ShouldBe(1);
    // }

    // [TestMethod]
    // public void GrantTickets_SelfServiceDisabled_ReturnsTicketNotAvailable()
    // {
    //     // Given
    //     var sut = NewRegistration();
    //
    //     var id = new TicketTypeId(new Guid("12345678-1234-1234-1234-1234567890ab"));
    //     var catalog = Catalog(
    //         Snapshot(id, slots: ["conference"], selfService: true, enabled: false)
    //     );
    //
    //     // When
    //     var result = sut.GrantTickets([id], catalog, TicketGrantMode.SelfService);
    //
    //     // Then
    //     result.IsSuccess.ShouldBeFalse();
    //     result.Error.ShouldBe(DomainErrors.TicketTypeNotAvailable(id));
    //
    //     sut.GetGrantedTicketTypeIds().ShouldBeEmpty();
    // }
    //
    // [TestMethod]
    // public void GrantTickets_AdminGrant_IgnoresSelfServiceRestrictions()
    // {
    //     // Given
    //     var sut = NewRegistration();
    //
    //     var id = new TicketTypeId(new Guid("0b0b0b0b-0b0b-0b0b-0b0b-0b0b0b0b0b0b"));
    //     var catalog = Catalog(
    //         Snapshot(id, slots: ["conference"], selfService: false, enabled: false)
    //     );
    //
    //     // When
    //     var result = sut.GrantTickets([id], catalog, TicketGrantMode.AdminGrant);
    //
    //     // Then
    //     result.IsSuccess.ShouldBeTrue();
    //
    //     sut.GetGrantedTicketTypeIds()
    //         .ShouldBe([id], ignoreOrder: true);
    // }
    
    #endregion

    private static Registration NewRegistration(
        TicketedEventId? eventId = null,
        EmailAddress? email = null)
    {
        return Registration.Create(
            eventId ?? DefaultEventId,
            email ?? DefaultEmail);
    }

    private static TicketRequest NewTicketRequest(
        TicketTypeSnapshot ticketType,
        TicketGrantMode? grantMode = null,
        CapacityEnforcementMode? capacityEnforcementMode = null)
    {
        grantMode ??= TicketGrantMode.Privileged;
        capacityEnforcementMode ??= CapacityEnforcementMode.Enforce;

        return new TicketRequest(ticketType.Id, grantMode.Value, capacityEnforcementMode.Value);
    }
    
    private static TicketTypeSnapshot NewTicketTypeSnapshot(
        TicketTypeId? id = null,
        TimeSlot[]? timeSlots = null,
        bool selfService = true,
        bool enabled = true)
    {
        id ??= TicketTypeId.New();
        timeSlots ??= [DefaultTimeSlot];
        return new TicketTypeSnapshot(id.Value, timeSlots);
    }

    private static Dictionary<TicketTypeId, TicketTypeSnapshot> Catalog(
        params TicketTypeSnapshot[] ticketTypeSnapshots)
        => ticketTypeSnapshots.ToDictionary(x => x.Id, x => x);
}