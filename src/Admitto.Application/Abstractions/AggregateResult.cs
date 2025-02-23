namespace Amolenk.Admitto.Application.Dtos;

public record AggregateResult<T>(T Aggregate, string? Etag = null);
