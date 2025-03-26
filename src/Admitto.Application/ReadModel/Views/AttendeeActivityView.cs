namespace Amolenk.Admitto.Application.ReadModel.Views;

public record AttendeeActivityView(Guid Id, Guid AttendeeId, string Activity, DateTime Timestamp);