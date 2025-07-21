using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.GetAttendee;

public record GetAttendeeResponse(string Email, AttendeeStatus Status);
