namespace Amolenk.Admitto.Application.Projections.Attendance;

public class AttendanceView
{
    public required Guid TeamId { get; init; }
    
    public required Guid TicketedEventId { get; init; }

    public required Guid AttendeeId { get; init; }

    public required AttendanceType AttendanceType { get; set; }

    public required uint AttendeeVersion { get; init; }
}

public enum AttendanceType
{
    CheckedIn,
    Canceled,
    CanceledLate,
    NoShow
}