using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.GetAttendees;

public record GetAttendeesResponse(AttendeeDto[] Attendees);

public record AttendeeDto(
    Guid AttendeeId,
    string Email,
    string FirstName,
    string LastName,
    RegistrationStatus Status,
    AdditionalDetailDto[] AdditionalDetails,
    TicketSelectionDto[] Tickets,
    DateTimeOffset LastChangedAt);

public record AdditionalDetailDto(string Name, string Value);

public record TicketSelectionDto(string TicketTypeSlug, int Quantity);
