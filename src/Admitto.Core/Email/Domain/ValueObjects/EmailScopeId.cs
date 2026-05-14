using Vogen;

namespace Amolenk.Admitto.Core.Email.Domain.ValueObjects;

[ValueObject<Guid>]
public partial struct EmailScopeId
{
    public static EmailScopeId New() => From(Guid.NewGuid());
}
