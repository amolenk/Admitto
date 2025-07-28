namespace Amolenk.Admitto.Domain;

public class DomainRuleException(DomainRuleError error) : Exception(error.MessageText)
{
    public string ErrorCode { get; } = error.Code;
}