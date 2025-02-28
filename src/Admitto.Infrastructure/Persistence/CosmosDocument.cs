using System.Text.Json.Serialization;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Infrastructure.Persistence;

public record CosmosDocument<T>
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("partitionKey")]
    public required string PartitionKey { get; init; }

    [JsonPropertyName("$type")]
    public required string Discriminator { get; init; }
    
    [JsonPropertyName("payload")]
    public required T Payload { get; init; }
}
