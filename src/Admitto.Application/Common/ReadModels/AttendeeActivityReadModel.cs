namespace Amolenk.Admitto.Application.Common.ReadModels;

public record AttendeeActivityReadModel(Guid Id, Guid AttendeeId, string Activity, DateTime Timestamp);