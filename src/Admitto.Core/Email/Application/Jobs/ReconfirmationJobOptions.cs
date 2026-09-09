using System.Globalization;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.Email.Application.Jobs;

public sealed class ReconfirmationJobOptions
{
    public const string SectionName = "Email:Reconfirmation";
    public const string IntervalConfigurationKey = $"{SectionName}:Interval";
    public static readonly TimeSpan MinimumInterval = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(1);

    /// <summary>
    /// The interval captured when the Worker starts. Changing it requires a Worker restart.
    /// </summary>
    public TimeSpan Interval { get; init; } = DefaultInterval;

    public static ReconfirmationJobOptions Parse(string? rawInterval)
    {
        if (string.IsNullOrWhiteSpace(rawInterval))
        {
            throw new OptionsValidationException(
                SectionName,
                typeof(ReconfirmationJobOptions),
                ["The reconfirmation interval is required."]);
        }

        if (!TimeSpan.TryParse(rawInterval, CultureInfo.InvariantCulture, out var interval))
        {
            throw new OptionsValidationException(
                SectionName,
                typeof(ReconfirmationJobOptions),
                ["The reconfirmation interval must be a valid duration."]);
        }

        if (interval < MinimumInterval)
        {
            throw new OptionsValidationException(
                SectionName,
                typeof(ReconfirmationJobOptions),
                ["The reconfirmation interval must be at least 00:01:00."]);
        }

        return new ReconfirmationJobOptions { Interval = interval };
    }
}
