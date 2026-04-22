using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

/// <summary>
/// Ordered, validated collection of <see cref="AdditionalDetailField"/> entries owned by a
/// <see cref="Entities.TicketedEvent"/>. Order is significant — display order is the index.
/// </summary>
public sealed record AdditionalDetailSchema
{
    public const int MaxFields = 25;

    public IReadOnlyList<AdditionalDetailField> Fields { get; }

    private AdditionalDetailSchema(IReadOnlyList<AdditionalDetailField> fields)
    {
        Fields = fields;
    }

    public static AdditionalDetailSchema Empty { get; } = new([]);

    public static AdditionalDetailSchema Create(IReadOnlyList<AdditionalDetailField> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        if (fields.Count > MaxFields)
            throw new BusinessRuleViolationException(Errors.TooManyFields);

        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
        {
            if (!seenKeys.Add(field.Key))
                throw new BusinessRuleViolationException(Errors.DuplicateKey(field.Key));

            if (!seenNames.Add(field.Name))
                throw new BusinessRuleViolationException(Errors.DuplicateName(field.Name));
        }

        return new AdditionalDetailSchema(fields.ToArray());
    }

    public bool TryGetField(string key, out AdditionalDetailField field)
    {
        for (var i = 0; i < Fields.Count; i++)
        {
            if (string.Equals(Fields[i].Key, key, StringComparison.Ordinal))
            {
                field = Fields[i];
                return true;
            }
        }

        field = null!;
        return false;
    }

    internal static class Errors
    {
        public static readonly Error TooManyFields = new(
            "additional_detail_schema.too_many_fields",
            $"An event may have at most {MaxFields} additional detail fields.",
            Type: ErrorType.Validation);

        public static Error DuplicateKey(string key) => new(
            "additional_detail_schema.duplicate_key",
            $"Field key '{key}' is duplicated.",
            Details: new Dictionary<string, object?> { ["key"] = key },
            Type: ErrorType.Validation);

        public static Error DuplicateName(string name) => new(
            "additional_detail_schema.duplicate_name",
            $"Field name '{name}' is duplicated (case-insensitive).",
            Details: new Dictionary<string, object?> { ["name"] = name },
            Type: ErrorType.Validation);
    }
}
