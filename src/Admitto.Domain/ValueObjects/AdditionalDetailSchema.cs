namespace Amolenk.Admitto.Domain.ValueObjects;

public record AdditionalDetailSchema(string Name, int MaxLength, bool IsRequired)
{
    public bool IsValid(string value)
    {
        return value.Length <= MaxLength;
    }
}
