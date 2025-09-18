using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Domain.Tests.TestHelpers.Builders;

public class AdditionalDetailSchemaBuilder
{
    private string _name = "General Admission";
    private int _maxLength = 50;
    private bool _isRequired = false;

    public AdditionalDetailSchemaBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AdditionalDetailSchemaBuilder WithMaxLength(int maxLength)
    {
        _maxLength = maxLength;
        return this;
    }
    
    public AdditionalDetailSchemaBuilder WithIsRequired(bool isRequired)
    {
        _isRequired = isRequired;
        return this;
    }

    public AdditionalDetailSchema Build() => new AdditionalDetailSchema(_name, _maxLength, _isRequired);
}