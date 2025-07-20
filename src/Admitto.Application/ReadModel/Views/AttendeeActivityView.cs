namespace Amolenk.Admitto.Application.ReadModel.Views;

public record AttendeeActivityView(
    Guid Id,
    Guid TeamId,
    Guid EventId,
    string Email,
    string Activity,
    DateTime Timestamp);