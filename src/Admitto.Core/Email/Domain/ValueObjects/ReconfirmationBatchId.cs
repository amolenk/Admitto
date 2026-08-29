namespace Amolenk.Admitto.Core.Email.Domain.ValueObjects;

public readonly record struct ReconfirmationBatchId(Guid Value)
{
    public static ReconfirmationBatchId New() => new(Guid.NewGuid());

    public static ReconfirmationBatchId From(Guid value) => new(value);

}
