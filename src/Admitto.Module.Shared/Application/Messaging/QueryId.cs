namespace Amolenk.Admitto.Module.Shared.Application.Messaging;

public readonly record struct QueryId(Guid Value)
{
    public static QueryId New() => new(Guid.NewGuid());
}