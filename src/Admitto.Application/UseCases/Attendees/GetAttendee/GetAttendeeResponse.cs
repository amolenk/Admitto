using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.GetAttendee;

public record GetAttendeeResponse(
    Guid AttendeeId,
    string Email,
    string FirstName,
    string LastName,
    RegistrationStatus RegistrationStatus,
    AdditionalDetailDto[] AdditionalDetails,
    TicketSelectionDto[] Tickets,
    ActivityDto[] Activities,
    DateTimeOffset LastChangedAt);

public record AdditionalDetailDto(string Name, string Value);

public record TicketSelectionDto(string TicketTypeSlug, int Quantity);

public record ActivityDto(DateTimeOffset OccuredOn, string Activity, string? EmailType);
