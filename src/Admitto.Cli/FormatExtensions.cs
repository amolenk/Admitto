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

    public static string FormatContributorRole(this ContributorRole? role)
    {
        return role switch
        {
            ContributorRole.Crew => "👷️ Crew",
            ContributorRole.Speaker => "🎤 Speaker",
            ContributorRole.Sponsor => "💰️ Sponsor",
            _ => $"❤️ {role.Humanize()}"
        };
    }

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