using System.Text.Json;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.EntityConfigurations;

internal static class AdditionalDetailJsonConverters
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General);

    public static readonly ValueConverter<AdditionalDetailSchema, string> SchemaConverter = new(
        v => SerializeSchema(v),
        s => DeserializeSchema(s));

    public static readonly ValueConverter<AdditionalDetails, string> DetailsConverter = new(
        v => JsonSerializer.Serialize((IReadOnlyDictionary<string, string>)v, Options),
        s => DeserializeDetails(s));

    private static string SerializeSchema(AdditionalDetailSchema schema)
    {
        var dto = schema.Fields.Select(f => new FieldDto(f.Key, f.Name, f.MaxLength)).ToArray();
        return JsonSerializer.Serialize(dto, Options);
    }

    private static AdditionalDetailSchema DeserializeSchema(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
            return AdditionalDetailSchema.Empty;

        var dto = JsonSerializer.Deserialize<FieldDto[]>(json, Options) ?? [];
        var fields = dto.Select(f => AdditionalDetailField.Create(f.Key, f.Name, f.MaxLength)).ToArray();
        return AdditionalDetailSchema.Create(fields);
    }

    private static AdditionalDetails DeserializeDetails(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
            return AdditionalDetails.Empty;

        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, Options);
        return AdditionalDetails.From(dict);
    }

    private sealed record FieldDto(string Key, string Name, int MaxLength);
}
