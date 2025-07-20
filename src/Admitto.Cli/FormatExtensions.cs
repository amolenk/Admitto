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

    public static string Format(this RegistrationRequestStatus status)
    {
        return $"[{GetStatusColor(status)}]{status.Humanize()}[/]";
    }
    
    private static string GetStatusColor(RegistrationRequestStatus status)
    {
        return status switch
        {
            RegistrationRequestStatus.Unverified => "yellow",
            RegistrationRequestStatus.Verified => "green",
            RegistrationRequestStatus.Accepted => "green",
            RegistrationRequestStatus.Rejected => "red",
            _ => "white"
        };
    }
}