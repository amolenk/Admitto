using Vogen;

namespace Amolenk.Admitto.Core.Email.Domain.ValueObjects;

[ValueObject<Guid>]
public partial struct BulkEmailJobId
{
    public static BulkEmailJobId New() => From(Guid.NewGuid());

    private static Validation Validate(Guid value)
        => value != Guid.Empty ? Validation.Ok : Validation.Invalid("Bulk email job ID cannot be empty.");
}

