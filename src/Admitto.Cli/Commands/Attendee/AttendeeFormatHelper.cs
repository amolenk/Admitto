using Amolenk.Admitto.Cli.Api;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public static class AttendeeFormatHelper
{
    public static string Format(this RegistrationStatus status)
    {
        return $"[{GetStatusColor(status)}]{status.Humanize()}[/]";
    }

    private static string GetStatusColor(RegistrationStatus status)
    {
        return status switch
        {
            RegistrationStatus.Reconfirmed => "green",
            RegistrationStatus.CheckedIn => "green",
            RegistrationStatus.Canceled => "red",
            RegistrationStatus.NoShow => "red",
            _ => "white"
        };
    }
}