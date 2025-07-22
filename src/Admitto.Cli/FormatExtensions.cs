using Humanizer;

namespace Amolenk.Admitto.Cli;

public static class FormatExtensions
{
    public static string Format(this DateTimeOffset dateTime, bool humanize = false)
    {
        var localTime = dateTime.ToLocalTime();

        if (humanize)
        {
            return $"{localTime:f} ({localTime.Humanize()})";
        }

        return localTime.ToString("f");
    }

    public static string Format(this AttendeeStatus status)
    {
        return $"[{GetStatusColor(status)}]{status.Humanize()}[/]";
    }
    
    private static string GetStatusColor(AttendeeStatus status)
    {
        return status switch
        {
            AttendeeStatus.PendingVerification => "yellow",
            AttendeeStatus.VerificationFailed => "red",
            AttendeeStatus.PendingTickets => "yellow",
            AttendeeStatus.Registered => "green",
            AttendeeStatus.RegistrationFailed => "red",
            AttendeeStatus.Canceled => "red",
            _ => "white"
        };
    }
}