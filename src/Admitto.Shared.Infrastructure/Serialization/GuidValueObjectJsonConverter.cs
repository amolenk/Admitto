// using System.Text.Json;
// using System.Text.Json.Serialization;
// using System.Reflection;
// using Amolenk.Admitto.Shared.Kernel.ValueObjects;
//
// namespace Amolenk.Admitto.Shared.Infrastructure.Serialization;
//
// public sealed class GuidValueObjectJsonConverter<T> : JsonConverter<T>
//     where T : struct, IGuidValueObject
// {
//     private static readonly ConstructorInfo Ctor =
//         typeof(T).GetConstructor([typeof(Guid)])
//         ?? throw new InvalidOperationException(
//             $"{typeof(T).Name} must have a public constructor with a single Guid parameter.");
//
//     public override T Read(
//         ref Utf8JsonReader reader,
//         Type typeToConvert,
//         JsonSerializerOptions options)
//     {
//         if (reader.TokenType != JsonTokenType.String)
//         {
//             throw new JsonException("Expected GUID string.");
//         }
//
//         var guid = reader.GetGuid();
//         return (T)Ctor.Invoke([guid]);
//     }
//
//     public override void Write(
//         Utf8JsonWriter writer,
//         T value,
//         JsonSerializerOptions options)
//     {
//         writer.WriteStringValue(value.Value);
//     }
// }