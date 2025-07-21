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
            AttendeeStatus.Unverified => "yellow",
            AttendeeStatus.Verified => "yellow",
            AttendeeStatus.VerificationFailed => "red",
            AttendeeStatus.Registered => "green",
            AttendeeStatus.Rejected => "grey",
            AttendeeStatus.Reconfirmed => "green",
            AttendeeStatus.CanceledOnTime => "grey",
            AttendeeStatus.CanceledLastMinute => "red",
            AttendeeStatus.SkippedEvent => "red",
            _ => "white"
        };
    }
}