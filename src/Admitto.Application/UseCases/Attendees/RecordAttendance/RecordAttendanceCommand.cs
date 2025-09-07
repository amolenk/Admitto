namespace Amolenk.Admitto.Application.UseCases.Attendees.RecordAttendance;

/// <summary>
/// Represents a command to record the attendance of a participant at a ticketed event.
/// </summary>
public record RecordAttendanceCommand(Guid AttendeeId, bool Attended) : Command;