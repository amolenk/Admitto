using Amolenk.Admitto.Cli.Api;

namespace Amolenk.Admitto.Cli.Commands.Events.BulkEmail;

/// <summary>
/// Shared CLI options for selecting bulk-email recipients. Exactly one of the
/// attendee criteria options OR <see cref="ExternalListPath"/> must be
/// supplied; the per-command validators enforce this.
/// </summary>
public abstract class BulkEmailSourceSettings : TeamEventSettings
{
    [CommandOption("--ticket-types")]
    [Description("Comma-separated ticket-type slugs to include.")]
    public string? TicketTypes { get; init; }

    [CommandOption("--status")]
    [Description("Registration status filter: 'registered' or 'cancelled'.")]
    public string? Status { get; init; }

    [CommandOption("--has-reconfirmed")]
    [Description("Filter by whether attendees have reconfirmed (true|false).")]
    public bool? HasReconfirmed { get; init; }

    [CommandOption("--registered-after")]
    [Description("Include only registrations created at or after this ISO 8601 timestamp.")]
    public string? RegisteredAfter { get; init; }

    [CommandOption("--registered-before")]
    [Description("Include only registrations created strictly before this ISO 8601 timestamp.")]
    public string? RegisteredBefore { get; init; }

    [CommandOption("--external-list")]
    [Description("Path (use '@file.csv') to a CSV file with one 'email[,displayName]' per line.")]
    public string? ExternalListPath { get; init; }

    public bool HasAttendeeCriteria =>
        !string.IsNullOrWhiteSpace(TicketTypes) ||
        !string.IsNullOrWhiteSpace(Status) ||
        HasReconfirmed.HasValue ||
        !string.IsNullOrWhiteSpace(RegisteredAfter) ||
        !string.IsNullOrWhiteSpace(RegisteredBefore);

    public bool HasExternalList => !string.IsNullOrWhiteSpace(ExternalListPath);

    public ValidationResult ValidateSource()
    {
        if (HasAttendeeCriteria && HasExternalList)
        {
            return ValidationResult.Error(
                "Specify EITHER attendee criteria (--ticket-types/--status/--has-reconfirmed/--registered-after/--registered-before) OR --external-list, not both.");
        }

        if (!HasAttendeeCriteria && !HasExternalList)
        {
            return ValidationResult.Error(
                "Specify a recipient source: attendee criteria (--ticket-types/--status/--has-reconfirmed/--registered-after/--registered-before) or --external-list @file.csv.");
        }

        if (!string.IsNullOrWhiteSpace(Status) &&
            !string.Equals(Status, "registered", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(Status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Error("--status must be 'registered' or 'cancelled'.");
        }

        if (!string.IsNullOrWhiteSpace(RegisteredAfter) && !DateTimeOffset.TryParse(RegisteredAfter, out _))
        {
            return ValidationResult.Error("--registered-after must be a valid ISO 8601 timestamp.");
        }

        if (!string.IsNullOrWhiteSpace(RegisteredBefore) && !DateTimeOffset.TryParse(RegisteredBefore, out _))
        {
            return ValidationResult.Error("--registered-before must be a valid ISO 8601 timestamp.");
        }

        return ValidationResult.Success();
    }

    public BulkEmailSourceCliRequest BuildSource()
    {
        if (HasExternalList)
        {
            var path = StripAtPrefix(ExternalListPath!);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"External list file not found: {path}", path);
            }

            var items = ParseCsv(File.ReadAllLines(path));
            if (items.Count == 0)
            {
                throw new InvalidOperationException(
                    $"External list file '{path}' contains no recipient rows.");
            }

            return new BulkEmailSourceCliRequest
            {
                ExternalList = new ExternalListSourceCliRequest { Items = items }
            };
        }

        var attendee = new AttendeeSourceCliRequest
        {
            TicketTypeSlugs = string.IsNullOrWhiteSpace(TicketTypes)
                ? null
                : TicketTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            RegistrationStatus = string.IsNullOrWhiteSpace(Status)
                ? null
                : Status!.ToLowerInvariant(),
            HasReconfirmed = HasReconfirmed,
            RegisteredAfter = string.IsNullOrWhiteSpace(RegisteredAfter)
                ? null
                : DateTimeOffset.Parse(RegisteredAfter!),
            RegisteredBefore = string.IsNullOrWhiteSpace(RegisteredBefore)
                ? null
                : DateTimeOffset.Parse(RegisteredBefore!),
        };

        return new BulkEmailSourceCliRequest { Attendee = attendee };
    }

    internal static string StripAtPrefix(string value) =>
        value.StartsWith('@') ? value[1..] : value;

    private static List<ExternalListRecipientCliRequest> ParseCsv(IEnumerable<string> lines)
    {
        var items = new List<ExternalListRecipientCliRequest>();
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            var parts = line.Split(',', 2, StringSplitOptions.TrimEntries);
            var email = parts[0];
            var displayName = parts.Length == 2 && parts[1].Length > 0 ? parts[1] : null;

            items.Add(new ExternalListRecipientCliRequest
            {
                Email = email,
                DisplayName = displayName
            });
        }

        return items;
    }
}
