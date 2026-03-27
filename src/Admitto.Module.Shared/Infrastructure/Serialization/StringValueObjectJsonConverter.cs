// using System.Text.Json;
// using System.Text.Json.Serialization;
// using System.Reflection;
// using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
//
// namespace Amolenk.Admitto.Module.Shared.Infrastructure.Serialization;
//
// public sealed class StringValueObjectJsonConverter<T> : JsonConverter<T>
//     where T : IStringValueObject
// {
//     private static readonly ConstructorInfo Ctor =
//         typeof(T).GetConstructor([typeof(string)])
//         ?? throw new InvalidOperationException(
//             $"{typeof(T).Name} must have a public constructor with a single String parameter.");
//
//     public override T Read(
//         ref Utf8JsonReader reader,
//         Type typeToConvert,
//         JsonSerializerOptions options)
//     {
//         if (reader.TokenType != JsonTokenType.String)
//         {
//             throw new JsonException("Expected string.");
//         }
//
//         return (T)Ctor.Invoke([reader.GetString()]);
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