namespace Amolenk.Admitto.Cli.Common;

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
}