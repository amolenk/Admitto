namespace Amolenk.Admitto.Domain;

public class BusinessRuleException(BusinessRuleError error) : Exception(error.MessageText)
{
    public string ErrorCode { get; } = error.Code;
}