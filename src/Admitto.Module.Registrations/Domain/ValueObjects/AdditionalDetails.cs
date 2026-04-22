using System.Collections;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

/// <summary>
/// Immutable bag of additional-detail values stored on a <see cref="Entities.Registration"/>,
/// keyed by <see cref="AdditionalDetailField.Key"/>. Values are arbitrary strings (including
/// the empty string). The presence/absence of a key, and value length, are validated against
/// the event's current <see cref="AdditionalDetailSchema"/> via <see cref="Validate"/>.
/// </summary>
public sealed record AdditionalDetails : IReadOnlyDictionary<string, string>
{
    private readonly IReadOnlyDictionary<string, string> _values;

    private AdditionalDetails(IReadOnlyDictionary<string, string> values)
    {
        _values = values;
    }

    public static AdditionalDetails Empty { get; } =
        new(new Dictionary<string, string>(StringComparer.Ordinal));

    public static AdditionalDetails From(IReadOnlyDictionary<string, string>? values)
    {
        if (values is null || values.Count == 0)
            return Empty;

        var dict = new Dictionary<string, string>(values.Count, StringComparer.Ordinal);
        foreach (var kvp in values)
            dict[kvp.Key] = kvp.Value ?? string.Empty;

        return new AdditionalDetails(dict);
    }

    /// <summary>
    /// Validates the supplied submission against the event's current schema, returning the
    /// accepted set of values. Any unknown key or value exceeding the field's MaxLength causes
    /// a <see cref="BusinessRuleViolationException"/>. Missing keys are accepted (treated as
    /// "not provided"); empty strings are preserved verbatim.
    /// </summary>
    public static AdditionalDetails Validate(
        IReadOnlyDictionary<string, string>? submission,
        AdditionalDetailSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        if (submission is null || submission.Count == 0)
            return Empty;

        var accepted = new Dictionary<string, string>(submission.Count, StringComparer.Ordinal);

        foreach (var kvp in submission)
        {
            if (!schema.TryGetField(kvp.Key, out var field))
                throw new BusinessRuleViolationException(Errors.KeyNotInSchema(kvp.Key));

            var value = kvp.Value ?? string.Empty;

            if (value.Length > field.MaxLength)
                throw new BusinessRuleViolationException(Errors.ValueTooLong(kvp.Key, field.MaxLength));

            accepted[kvp.Key] = value;
        }

        return new AdditionalDetails(accepted);
    }

    public int Count => _values.Count;
    public IEnumerable<string> Keys => _values.Keys;
    public IEnumerable<string> Values => _values.Values;
    public string this[string key] => _values[key];
    public bool ContainsKey(string key) => _values.ContainsKey(key);
    public bool TryGetValue(string key, out string value) => _values.TryGetValue(key, out value!);
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_values).GetEnumerator();

    public bool Equals(AdditionalDetails? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_values.Count != other._values.Count) return false;
        foreach (var kvp in _values)
        {
            if (!other._values.TryGetValue(kvp.Key, out var v) || v != kvp.Value) return false;
        }
        return true;
    }

    public override int GetHashCode()
    {
        var hash = 0;
        foreach (var kvp in _values)
            hash ^= HashCode.Combine(kvp.Key, kvp.Value);
        return hash;
    }

    internal static class Errors
    {
        public static Error KeyNotInSchema(string key) => new(
            "additional_details.key_not_in_schema",
            $"Additional detail key '{key}' is not declared on the event's schema.",
            Details: new Dictionary<string, object?> { ["key"] = key },
            Type: ErrorType.Validation);

        public static Error ValueTooLong(string key, int maxLength) => new(
            "additional_details.value_too_long",
            $"Additional detail value for '{key}' exceeds the field's maxLength of {maxLength}.",
            Details: new Dictionary<string, object?> { ["key"] = key, ["maxLength"] = maxLength },
            Type: ErrorType.Validation);
    }
}
