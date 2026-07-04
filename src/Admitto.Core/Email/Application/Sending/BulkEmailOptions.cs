namespace Amolenk.Admitto.Core.Email.Application.Sending;

/// <summary>
/// Options that govern bulk-email fan-out behaviour. Bound from configuration
/// section <c>BulkEmail</c> via the standard options pattern.
/// </summary>
public sealed class BulkEmailOptions
{
    public const string SectionName = "BulkEmail";

    /// <summary>
    /// Delay applied between consecutive recipient sends within a single
    /// fan-out pickup. Cancellation observed after the wait. Default 500ms.
    /// </summary>
    public TimeSpan PerMessageDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    public int InlineRetryCount { get; set; } = 2;
    public TimeSpan InlineRetryDelay { get; set; } = TimeSpan.FromMilliseconds(250);
}
