namespace Amolenk.Admitto.Api.Configuration;

public sealed class PublicRateLimitingOptions
{
    public const string SectionName = "RateLimiting:Public";

    public RateLimitPolicyOptions Strict { get; init; } = new()
    {
        PermitLimit = 120,
        WindowSeconds = 60,
        SegmentsPerWindow = 6,
        QueueLimit = 0
    };

    public RateLimitPolicyOptions Standard { get; init; } = new()
    {
        PermitLimit = 600,
        WindowSeconds = 60,
        SegmentsPerWindow = 6,
        QueueLimit = 0
    };
}

public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; init; } = 100;
    public int WindowSeconds { get; init; } = 60;
    public int SegmentsPerWindow { get; init; } = 6;
    public int QueueLimit { get; init; }
}
