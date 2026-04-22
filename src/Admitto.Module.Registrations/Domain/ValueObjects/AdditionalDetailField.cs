using System.Text.RegularExpressions;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

/// <summary>
/// Single configurable additional-detail field on a <see cref="Entities.TicketedEvent"/>.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><see cref="Key"/> is the stable storage identity (immutable once persisted).</item>
///   <item><see cref="Name"/> is the human-readable label (editable).</item>
///   <item><see cref="MaxLength"/> caps the length of stored values for new registrations.</item>
/// </list>
/// </remarks>
public sealed partial record AdditionalDetailField
{
    public const int NameMaxLength = 100;
    public const int KeyMaxLength = 50;
    public const int MaxValueLength = 4000;

    public string Key { get; }
    public string Name { get; }
    public int MaxLength { get; }

    private AdditionalDetailField(string key, string name, int maxLength)
    {
        Key = key;
        Name = name;
        MaxLength = maxLength;
    }

    public static AdditionalDetailField Create(string key, string name, int maxLength)
    {
        if (key is null || !KeyRegex().IsMatch(key))
            throw new BusinessRuleViolationException(Errors.InvalidKey);

        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleViolationException(Errors.NameEmpty);

        var trimmedName = name.Trim();

        if (trimmedName.Length > NameMaxLength)
            throw new BusinessRuleViolationException(Errors.NameTooLong);

        if (maxLength < 1 || maxLength > MaxValueLength)
            throw new BusinessRuleViolationException(Errors.MaxLengthOutOfRange);

        return new AdditionalDetailField(key, trimmedName, maxLength);
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,49}$")]
    private static partial Regex KeyRegex();

    internal static class Errors
    {
        public static readonly Error InvalidKey = new(
            "additional_detail_field.invalid_key",
            "Field key must match '^[a-z0-9][a-z0-9-]{0,49}$' (kebab-case, 1-50 chars).",
            Type: ErrorType.Validation);

        public static readonly Error NameEmpty = new(
            "additional_detail_field.name_empty",
            "Field name is required.",
            Type: ErrorType.Validation);

        public static readonly Error NameTooLong = new(
            "additional_detail_field.name_too_long",
            $"Field name must be at most {NameMaxLength} character(s).",
            Type: ErrorType.Validation);

        public static readonly Error MaxLengthOutOfRange = new(
            "additional_detail_field.max_length_out_of_range",
            $"Field maxLength must be between 1 and {MaxValueLength}.",
            Type: ErrorType.Validation);
    }
}
